// PropamaPOS/Controllers/AjustesController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;
using System.Security.Claims;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class AjustesController : Controller
    {
        private readonly AppDbContext _context;

        public AjustesController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Ajustes
        [Authorize(Roles = "Admin,Empleado")]
        public async Task<IActionResult> Index(DateTime? desde, DateTime? hasta, TipoMovimientoAjuste? tipo, string empleado, int page = 1)
        {
            const int PageSize = 30;

            var query = _context.AjustesInventario
                .Include(a => a.Empleado)
                .Include(a => a.Detalles)
                .AsQueryable();

            // Filtro por rango de fechas
            if (desde.HasValue)
                query = query.Where(a => a.Fecha >= desde.Value);
            if (hasta.HasValue)
                query = query.Where(a => a.Fecha <= hasta.Value);

            // Filtro por tipo
            if (tipo.HasValue)
                query = query.Where(a => a.Tipo == tipo.Value);

            // Filtro por empleado (texto libre)
            if (!string.IsNullOrWhiteSpace(empleado))
            {
                var emp = empleado.Trim().ToLower();
                query = query.Where(a =>
                    (a.Empleado != null && (
                        a.Empleado.Nombre.ToLower().Contains(emp) ||
                        (a.Empleado.Apellido != null && a.Empleado.Apellido.ToLower().Contains(emp))
                    )) ||
                    (a.CreadoPor != null && a.CreadoPor.ToLower().Contains(emp))
                );
            }

            // Orden descendente por fecha
            query = query.OrderByDescending(a => a.Fecha);

            // Total y paginación
            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var ajustes = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // ViewBags
            ViewBag.Desde = desde?.ToString("yyyy-MM-dd");
            ViewBag.Hasta = hasta?.ToString("yyyy-MM-dd");
            ViewBag.CurrentTipo = tipo;
            ViewBag.CurrentEmpleado = empleado;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;
            ViewBag.PageSize = PageSize;

            return View(ajustes);
        }

        // GET: Ajustes/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var ajuste = await _context.Set<AjusteInventario>()
                .Include(a => a.Empleado)
                .Include(a => a.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(a => a.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(a => a.Id_Ajuste == id);

            if (ajuste == null) return NotFound();
            return View(ajuste);
        }

        // GET: Ajustes/Crear
        public async Task<IActionResult> Crear()
        {
            // Cargar items proyectados (misma forma que en Compras para JS)
            var items = await _context.Items
                .Where(i => i.Activo && !i.IsServicio)
                .Include(i => i.Presentaciones)
                    .ThenInclude(p => p.UnidadMedida)
                .Select(i => new
                {
                    id_Item = i.Id_Item,
                    nombre = i.Nombre,
                    presentaciones = i.Presentaciones.Select(p => new
                    {
                        id_ItemPresentacion = p.Id_ItemPresentacion,
                        cantidad = p.Cantidad,
                        unidadMedida = new
                        {
                            id_UnidadMedida = p.UnidadMedida.Id_UnidadMedida,
                            nombre = p.UnidadMedida.Nombre
                        }
                    })
                })
                .ToListAsync();

            ViewBag.Items = items;
            return View(new AjusteCrearViewModel { Fecha = DateTime.Now });
        }

        // POST: Ajustes/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(AjusteCrearViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarViewBags();
                return View(model);
            }

            if (model.Lineas == null || !model.Lineas.Any(l => l.Id_Item != null && l.Id_ItemPresentacion != null))
            {
                ModelState.AddModelError("", "Debe agregar al menos una línea al ajuste antes de guardarlo.");
                await CargarViewBags();
                return View(model);
            }

            // Determinar sign por tipo
            int signFromTipo(TipoMovimientoAjuste t)
            {
                return t == TipoMovimientoAjuste.Entrada ? 1 : -1;
            }
            int globalSign = signFromTipo(model.Tipo);

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                // obtener empleado si existe
                int? empleadoId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int idUsuario))
                {
                    var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id_Usuario == idUsuario);
                    if (empleado != null) empleadoId = empleado.Id_Empleado;
                }

                var ajuste = new AjusteInventario
                {
                    Fecha = model.Fecha ?? DateTime.UtcNow,
                    Tipo = model.Tipo,
                    Motivo = model.Motivo,
                    Observaciones = model.Observaciones,
                    CreadoPor = User.Identity?.Name ?? "Sistema",
                    Id_Empleado = empleadoId
                };

                _context.Add(ajuste);
                await _context.SaveChangesAsync();

                // validar y aplicar cada linea
                foreach (var linea in model.Lineas)
                {
                    if (linea.Id_Item == null || linea.Id_ItemPresentacion == null) continue;

                    var presentacion = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == linea.Id_ItemPresentacion.Value);

                    if (presentacion == null) continue;

                    var item = presentacion.Item;
                    int unidades = linea.CantidadPresentaciones * presentacion.Cantidad;
                    int unidadesSigned = unidades * globalSign;

                    int stockPrevio = item.Stock;
                    int stockDespues = stockPrevio + unidadesSigned;

                    if (stockDespues < 0)
                    {
                        ModelState.AddModelError("", $"No hay suficiente stock para {item.Nombre}. Stock actual: {stockPrevio}, intentado remover: {Math.Abs(unidadesSigned)}");
                        await trx.RollbackAsync();
                        await CargarViewBags();
                        return View(model);
                    }

                    // actualizar stock
                    item.Stock = stockDespues;
                    _context.Items.Update(item);

                    var detalle = new AjusteInventarioDetalle
                    {
                        Id_Ajuste = ajuste.Id_Ajuste,
                        Id_Item = item.Id_Item,
                        Id_ItemPresentacion = presentacion.Id_ItemPresentacion,
                        CantidadPresentaciones = linea.CantidadPresentaciones,
                        CantidadUnidades = unidadesSigned,
                        StockAntes = stockPrevio,
                        StockDespues = stockDespues,
                        Nota = linea.Nota
                    };

                    _context.Add(detalle);
                }

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Ajuste registrado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                ModelState.AddModelError("", $"Error al guardar ajuste: {ex.Message}");
                await CargarViewBags();
                return View(model);
            }
        }

        private async Task CargarViewBags()
        {
            var items = await _context.Items
                .Where(i => i.Activo && !i.IsServicio)
                .Include(i => i.Presentaciones)
                    .ThenInclude(p => p.UnidadMedida)
                .Select(i => new
                {
                    id_Item = i.Id_Item,
                    nombre = i.Nombre,
                    presentaciones = i.Presentaciones.Select(p => new
                    {
                        id_ItemPresentacion = p.Id_ItemPresentacion,
                        cantidad = p.Cantidad,
                        unidadMedida = new
                        {
                            id_UnidadMedida = p.UnidadMedida.Id_UnidadMedida,
                            nombre = p.UnidadMedida.Nombre
                        }
                    })
                })
                .ToListAsync();

            ViewBag.Items = items;
        }
    }
}
