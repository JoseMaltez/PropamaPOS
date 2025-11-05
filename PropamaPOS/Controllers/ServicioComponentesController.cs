using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class ServicioComponentesController : Controller
    {
        private readonly AppDbContext _context;
        public ServicioComponentesController(AppDbContext context) => _context = context;

        // GET: ServicioComponentes
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Index(string q, int page = 1)
        {
            const int PageSize = 30;

            var serviciosQuery = _context.Items
                .Where(i => i.Activo && i.IsServicio)
                .Include(i => i.ServicioComponentes!)
                    .ThenInclude(sc => sc.ItemConsumido)
                .AsQueryable();

            // Filtro de texto (nombre del servicio o del insumo consumido)
            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim().ToLower();
                serviciosQuery = serviciosQuery.Where(s =>
                    s.Nombre.ToLower().Contains(q) ||
                    s.ServicioComponentes.Any(c => c.ItemConsumido!.Nombre.ToLower().Contains(q))
                );
            }

            // Paginación
            var total = await serviciosQuery.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var servicios = await serviciosQuery
                .OrderBy(s => s.Nombre)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ViewBags para la vista
            ViewBag.CurrentQuery = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;
            ViewBag.PageSize = PageSize;

            return View(servicios);
        }



        // GET: ServicioComponentes/CrearMultiple
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CrearMultiple(int? servicioId)
        {
            ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
            ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();

            var model = new PropamaPOS.Models.ViewModels.ServicioComponentesCrearViewModel();
            if (servicioId.HasValue) model.Id_Servicio = servicioId.Value;
            // Inicializa con una línea vacía para que la UI muestre una fila
            model.Lineas.Add(new PropamaPOS.Models.ViewModels.ServicioComponenteLineaViewModel());
            return View(model);
        }

        // POST: ServicioComponentes/CrearMultiple
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CrearMultiple(PropamaPOS.Models.ViewModels.ServicioComponentesCrearViewModel model)
        {
            ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
            ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();

            if (!ModelState.IsValid)
            {
                TempData["ErrorMessage"] = "Faltan datos o hay errores en el formulario.";
                return View(model);
            }

            // Validar servicio existe y es servicio
            var servicio = await _context.Items.FirstOrDefaultAsync(i => i.Id_Item == model.Id_Servicio && i.IsServicio);
            if (servicio == null)
            {
                ModelState.AddModelError("", "Servicio no válido.");
                return View(model);
            }

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                foreach (var linea in model.Lineas)
                {
                    if (linea == null) continue;

                    var insumo = await _context.Items.FirstOrDefaultAsync(i => i.Id_Item == linea.Id_Item && !i.IsServicio);
                    if (insumo == null)
                    {
                        await trx.RollbackAsync();
                        ModelState.AddModelError("", $"Insumo inválido (Id {linea.Id_Item}).");
                        return View(model);
                    }

                    var sc = new ServicioComponente
                    {
                        Id_Servicio = model.Id_Servicio,
                        Id_Item = linea.Id_Item,
                        CantidadPorServicio = linea.CantidadPorServicio
                    };
                    _context.ServicioComponentes.Add(sc);
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Componentes agregados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                TempData["ErrorMessage"] = $"Error al guardar: {ex.Message}";
                return View(model);
            }
        }

        // GET: ServicioComponentes/EditarServicio/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarServicio(int id)
        {
            var servicio = await _context.Items.FirstOrDefaultAsync(i => i.Id_Item == id && i.IsServicio);
            if (servicio == null) return NotFound();

            var componentes = await _context.ServicioComponentes
                .Include(sc => sc.ItemConsumido)
                .Where(sc => sc.Id_Servicio == id)
                .OrderBy(sc => sc.ItemConsumido.Nombre)
                .ToListAsync();

            ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
            ViewBag.ServicioNombre = servicio.Nombre;
            ViewBag.ServicioId = servicio.Id_Item;

            return View(componentes);
        }

        // POST: ServicioComponentes/EditarServicio/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EditarServicio(int id, List<ServicioComponente> componentes)
        {
            var servicio = await _context.Items.FirstOrDefaultAsync(i => i.Id_Item == id && i.IsServicio);
            if (servicio == null) return NotFound();

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                // eliminar todos los componentes existentes del servicio
                var existentes = _context.ServicioComponentes.Where(sc => sc.Id_Servicio == id);
                _context.ServicioComponentes.RemoveRange(existentes);

                // volver a insertar los que vienen del formulario
                foreach (var comp in componentes)
                {
                    if (comp.Id_Item <= 0 || comp.CantidadPorServicio <= 0) continue;
                    comp.Id_Servicio = id;
                    _context.ServicioComponentes.Add(comp);
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Componentes del servicio actualizados correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                TempData["ErrorMessage"] = $"Error al guardar: {ex.Message}";
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: ServicioComponentes/EliminarServicio/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarServicio(int id)
        {
            var servicio = await _context.Items
                .Include(s => s.ServicioComponentes!)
                    .ThenInclude(sc => sc.ItemConsumido)
                .FirstOrDefaultAsync(s => s.Id_Item == id && s.IsServicio);

            if (servicio == null) return NotFound();
            return View(servicio);
        }

        // POST: ServicioComponentes/EliminarServicio/5
        [HttpPost]
        [ActionName("EliminarServicio")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarServicioConfirmado(int id)
        {
            var servicio = await _context.Items
                .Include(s => s.ServicioComponentes)
                .FirstOrDefaultAsync(s => s.Id_Item == id && s.IsServicio);

            if (servicio != null)
            {
                _context.ServicioComponentes.RemoveRange(servicio.ServicioComponentes!);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Componentes del servicio '{servicio.Nombre}' eliminados.";
            }

            return RedirectToAction(nameof(Index));
        }


        [HttpGet]
        public async Task<IActionResult> CheckStock(int idServicio, int cantidad)
        {
            var componentes = await _context.ServicioComponentes
                .Include(sc => sc.ItemConsumido)
                .Where(sc => sc.Id_Servicio == idServicio)
                .ToListAsync();

            var result = componentes.Select(c => new {
                id = c.Id_ServicioComponente,
                itemId = c.Id_Item,
                nombre = c.ItemConsumido?.Nombre ?? "",
                stock = c.ItemConsumido?.Stock ?? 0,
                necesarioPorUnidad = c.CantidadPorServicio,
                necesarioTotal = c.CantidadPorServicio * cantidad
            });

            return Json(result);
        }
    }
}
