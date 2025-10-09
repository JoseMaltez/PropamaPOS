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

        // Listado de compras (historial)
        public async Task<IActionResult> Index()
        {
            var compras = await _context.Compras
                .Include(c => c.Proveedor)
                .OrderByDescending(c => c.Fecha)
                .ToListAsync();

            return View(compras);
        }

        // Ver detalle de una compra
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

        // 🧾 Mostrar formulario para crear nueva compra
        public async Task<IActionResult> Crear()
        {
            ViewBag.Proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .OrderBy(p => p.Nombre)
                .ToListAsync();

            var items = await _context.Items
                .Where(i => i.Activo)
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


        // 💾 Procesar la compra (POST)
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
                var compra = new Compra
                {
                    Fecha = model.Fecha ?? DateTime.UtcNow,
                    Id_Proveedor = model.Id_Proveedor,
                    CreadoPor = User.Identity?.Name ?? "Administrador",
                    Nota = model.Nota
                };

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                decimal total = 0m;
                decimal markup = _config.GetValue<decimal?>("Pricing:DefaultMarkup") ?? 0.30m;

                foreach (var linea in model.Lineas)
                {
                    var presentacion = await _context.ItemPresentaciones
                        .Include(p => p.Item)
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

                    // Actualizar datos del producto
                    item.Stock = nuevoStock;
                    item.CostoPromedioUnidad = nuevoCostoPromedio;

                    // Actualizar datos de la presentación
                    presentacion.PrecioCosto = linea.PrecioCostoPorPresentacion;
                    presentacion.PrecioVenta = Math.Round(linea.PrecioCostoPorPresentacion * (1 + markup), 2);

                    // Crear detalle de compra
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
                    _context.Items.Update(item);
                    _context.ItemPresentaciones.Update(presentacion);
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

        // Método auxiliar para recargar listas en caso de error de validación
        private async Task CargarViewBags()
        {
            ViewBag.Proveedores = await _context.Proveedores
            .Where(p => p.Activo)
            .OrderBy(p => p.Nombre)
            .ToListAsync();

            var items = await _context.Items
                .Where(i => i.Activo)
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
