using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;
using Microsoft.Extensions.Configuration;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ComprasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public ComprasController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Compras
        public async Task<IActionResult> Index()
        {
            var compras = await _context.Compras
                .Include(c => c.Proveedor)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            return View(compras);
        }

        // GET: Compras/Details/5
        public async Task<IActionResult> Details(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(c => c.Id_Compra == id);

            if (compra == null)
                return NotFound();

            return View(compra);
        }

        // GET: Compras/Crear
        public async Task<IActionResult> Crear()
        {
            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

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
                        precioVenta = p.PrecioVenta,
                        unidadMedida = new
                        {
                            id_UnidadMedida = p.UnidadMedida.Id_UnidadMedida,
                            nombre = p.UnidadMedida.Nombre
                        }
                    })
                })
                .ToListAsync();

            ViewBag.Items = items;

            return View(new CompraCrearViewModel());
        }


        // POST: Compras/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(CompraCrearViewModel model)
        {
            if (!ModelState.IsValid)
            {
                await CargarViewBags();
                return View(model);
            }

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                // Intentar obtener el empleado desde el usuario autenticado
                int? empleadoId = null;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int idUsuario))
                {
                    var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id_Usuario == idUsuario);
                    if (empleado != null) empleadoId = empleado.Id_Empleado;
                }

                var compra = new Compra
                {
                    Fecha = model.Fecha ?? DateTime.UtcNow,
                    Id_Proveedor = model.Id_Proveedor,
                    CreadoPor = User.Identity?.Name ?? "Administrador",
                    Nota = model.Nota,
                    Id_Empleado = empleadoId
                };

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                decimal total = 0m;
                decimal markup = _config.GetValue<decimal?>("Pricing:DefaultMarkup") ?? 0.30m;
                decimal retailSurcharge = _config.GetValue<decimal?>("Pricing:RetailSurcharge") ?? 0.20m;
                // retailSurcharge: margen extra para ventas al detalle (presentaciones pequeñas)

                foreach (var linea in model.Lineas)
                {
                    var presentacion = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                            .ThenInclude(i => i.Presentaciones)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == linea.Id_ItemPresentacion);

                    if (presentacion == null)
                        continue;

                    var item = presentacion.Item;
                    int unidadesCompradas = linea.CantidadPresentaciones * presentacion.Cantidad;
                    decimal costoPorUnidad = Math.Round(linea.PrecioCostoPorPresentacion / presentacion.Cantidad, 4);

                    // Calcular nuevo costo promedio
                    int stockPrevio = item.Stock;
                    decimal costoPrevioTotal = stockPrevio * item.CostoPromedioUnidad;
                    decimal costoNuevoTotal = unidadesCompradas * costoPorUnidad;
                    int nuevoStock = stockPrevio + unidadesCompradas;
                    decimal nuevoCostoPromedio = nuevoStock == 0 ? costoPorUnidad :
                        Math.Round((costoPrevioTotal + costoNuevoTotal) / nuevoStock, 4);

                    // Actualizar stock y costo promedio
                    item.Stock = nuevoStock;
                    item.CostoPromedioUnidad = nuevoCostoPromedio;
                    _context.Items.Update(item);

                    // --- PROPAGAR PRECIOS A TODAS LAS PRESENTACIONES DEL ITEM ---
                    var allPres = item.Presentaciones.Where(p => p.Activo).ToList();
                    foreach (var pres in allPres)
                    {
                        // Nuevo costo por presentacion basado en costoPorUnidad
                        decimal costoPorPresentacion = Math.Round(costoPorUnidad * pres.Cantidad, 2);
                        pres.PrecioCosto = costoPorPresentacion;

                        // Determinar markup por presentación: si es venta al detalle (p.Cantidad == 1) aplicar surcharge
                        decimal extra = pres.Cantidad == 1 ? retailSurcharge : 0m;
                        decimal markupForPresentation = markup + extra;

                        pres.PrecioVenta = Math.Round(costoPorPresentacion * (1 + markupForPresentation), 2);

                        _context.ItemPresentaciones.Update(pres);
                    }

                    // Crear detalle de compra (registro histórico) usando la presentacion comprada
                    var detalle = new CompraDetalle
                    {
                        Id_Compra = compra.Id_Compra,
                        Id_Item = item.Id_Item,
                        Id_ItemPresentacion = presentacion.Id_ItemPresentacion,
                        CantidadPresentaciones = linea.CantidadPresentaciones,
                        PrecioCostoPorPresentacion = linea.PrecioCostoPorPresentacion,
                        PrecioCostoPorUnidad = costoPorUnidad,
                        Subtotal = Math.Round(linea.PrecioCostoPorPresentacion * linea.CantidadPresentaciones, 2)
                    };

                    _context.CompraDetalles.Add(detalle);
                    total += detalle.Subtotal;
                }

                compra.Total = total;
                _context.Compras.Update(compra);
                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Compra registrada exitosamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                ModelState.AddModelError("", $"Error al registrar la compra: {ex.Message}");
                await CargarViewBags();
                return View(model);
            }
        }

        private async Task CargarViewBags()
        {
            ViewBag.Proveedores = await _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

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
                        precioVenta = p.PrecioVenta,
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
