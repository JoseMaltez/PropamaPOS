using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ServicioComponentesController : Controller
    {
        private readonly AppDbContext _context;
        public ServicioComponentesController(AppDbContext context) => _context = context;

        // GET: ServicioComponentes
        public async Task<IActionResult> Index()
        {
            var list = await _context.ServicioComponentes
                .Include(s => s.Servicio)
                .Include(s => s.ItemConsumido)
                .OrderBy(sc => sc.Id_Servicio)
                .ToListAsync();
            return View(list);
        }

        // GET: ServicioComponentes/Crear
        public async Task<IActionResult> Crear(int? servicioId)
        {
            ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
            ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();

            var model = new ServicioComponente();
            if (servicioId.HasValue) model.Id_Servicio = servicioId.Value;
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ServicioComponente model)
        {
            // Depurar errores
            if (!ModelState.IsValid)
            {
                foreach (var kv in ModelState)
                {
                    foreach (var err in kv.Value.Errors)
                    {
                        Console.WriteLine($"Error en {kv.Key}: {err.ErrorMessage}");
                    }
                }

                ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                TempData["ErrorMessage"] = "Faltan datos o hay un error en el formulario.";
                return View(model);
            }

            try
            {
                // Validar existencia de servicio e insumo
                var servicio = await _context.Items.FirstOrDefaultAsync(i => i.Id_Item == model.Id_Servicio && i.IsServicio);
                var insumo = await _context.Items.FirstOrDefaultAsync(i => i.Id_Item == model.Id_Item && !i.IsServicio);

                if (servicio == null || insumo == null)
                {
                    TempData["ErrorMessage"] = "El servicio o el insumo seleccionado no existen o no son válidos.";
                    ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                    ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                    return View(model);
                }

                _context.ServicioComponentes.Add(model);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Componente agregado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al guardar componente: {ex.Message}");
                TempData["ErrorMessage"] = "Ocurrió un error al guardar el componente.";
                ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                return View(model);
            }
        }


        // GET: ServicioComponentes/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var sc = await _context.ServicioComponentes.FindAsync(id);
            if (sc == null) return NotFound();

            ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
            ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
            return View(sc);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ServicioComponente model)
        {
            if (id != model.Id_ServicioComponente) return NotFound();
            if (!ModelState.IsValid)
            {
                ViewBag.Servicios = await _context.Items.Where(i => i.Activo && i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                ViewBag.Items = await _context.Items.Where(i => i.Activo && !i.IsServicio).OrderBy(i => i.Nombre).ToListAsync();
                return View(model);
            }

            _context.Update(model);
            await _context.SaveChangesAsync();
            TempData["SuccessMessage"] = "Componente actualizado.";
            return RedirectToAction(nameof(Index));
        }

        // GET: ServicioComponentes/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var sc = await _context.ServicioComponentes
                .Include(s => s.Servicio)
                .Include(s => s.ItemConsumido)
                .FirstOrDefaultAsync(s => s.Id_ServicioComponente == id);
            if (sc == null) return NotFound();
            return View(sc);
        }

        [HttpPost, ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var sc = await _context.ServicioComponentes.FindAsync(id);
            if (sc != null)
            {
                _context.ServicioComponentes.Remove(sc);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = "Componente eliminado.";
            }
            return RedirectToAction(nameof(Index));
        }

        // Endpoint auxiliar para verificar stock (opcional, usado por AJAX desde la UI)
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
