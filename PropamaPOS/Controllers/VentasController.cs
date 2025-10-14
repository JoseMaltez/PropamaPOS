// PropamaPOS/Controllers/VentasController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels; // opcional si creas ViewModels
using System.Security.Claims;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class VentasController : Controller
    {
        private readonly AppDbContext _context;

        public VentasController(AppDbContext context)
        {
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            var ventas = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .OrderByDescending(v => v.Fecha)
                .ToListAsync();
            return View(ventas);
        }

        public async Task<IActionResult> Details(int id)
        {
            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Item)
                .Include(v => v.Pagos)
                .FirstOrDefaultAsync(v => v.Id_Venta == id);

            if (venta == null) return NotFound();
            return View(venta);
        }

        private async Task<string> GenerarNumeroVentaAsync()
        {
            int ultimoNumero = 0;
            var ultimaVenta = await _context.Ventas
                .OrderByDescending(v => v.Id_Venta)
                .FirstOrDefaultAsync();
            if (ultimaVenta != null && !string.IsNullOrEmpty(ultimaVenta.NumeroVenta))
            {
                var parte = ultimaVenta.NumeroVenta.Replace("FAC-", "");
                int.TryParse(parte, out ultimoNumero);
            }
            return $"FAC-{(ultimoNumero + 1).ToString("D6")}";
        }

        // GET: Ventas/Crear
        public async Task<IActionResult> Crear()
        {
            // Cargar items (productos y servicios)
            var items = await _context.Items
                .Where(i => i.Activo)
                .Include(i => i.Presentaciones)
                    .ThenInclude(p => p.UnidadMedida)
                .Select(i => new
                {
                    id_Item = i.Id_Item,
                    nombre = i.Nombre,
                    codigo = i.Codigo,
                    isServicio = i.IsServicio,
                    presentaciones = i.Presentaciones.Where(p => p.Activo).Select(p => new {
                        id_ItemPresentacion = p.Id_ItemPresentacion,
                        cantidad = p.Cantidad,
                        precioVenta = p.PrecioVenta,
                        unidad = p.UnidadMedida.Nombre,
                        stock = i.Stock
                    })
                })
                .ToListAsync();

            ViewBag.Items = items;
            ViewBag.Clientes = await _context.Clientes.Where(c => c.Activo).ToListAsync();
            return View();
        }

        // POST: Ventas/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear([FromForm] Venta ventaInput, [FromForm] List<VentaDetalle> lineas, string facturarCon, string nitInput, string nombreConsumidor)
        {
            // facturarCon: "NIT" o "CF" (Consumidor Final)
            if (lineas == null || !lineas.Any())
            {
                ModelState.AddModelError("", "La factura debe contener al menos un producto/servicio.");
                ViewBag.PreviousLineas = lineas;
                await CargarViewBagsCrear();
                return View();
            }

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                // empleado actual (si hay)
                int? empleadoId = null;
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int idUsuario))
                {
                    var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id_Usuario == idUsuario);
                    if (empleado != null) empleadoId = empleado.Id_Empleado;
                }

                // Cliente handling
                Cliente? cliente = null;
                if (facturarCon == "NIT")
                {
                    if (!string.IsNullOrEmpty(nitInput))
                    {
                        cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.NIT == nitInput);
                        if (cliente == null)
                        {
                            // Crear nuevo cliente usando campos de ventaInput.Cliente (si llenaste)
                            cliente = new Cliente
                            {
                                NIT = nitInput,
                                Nombre = ventaInput.Cliente?.Nombre ?? "N/A",
                                Apellido = ventaInput.Cliente?.Apellido,
                                Direccion = ventaInput.Cliente?.Direccion,
                                Activo = true
                            };
                            _context.Clientes.Add(cliente);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                else // Consumidor Final
                {
                    // no guardar cliente en DB, solo nombre opcional
                    ventaInput.Cliente = null;
                }

                // Validar stock por cada linea (sumar por presentacion)
                var insuficientes = new List<string>();
                foreach (var linea in lineas)
                {
                    if (linea.Id_ItemPresentacion == null) continue;
                    var pres = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == linea.Id_ItemPresentacion.Value);
                    if (pres == null) continue;
                    if (pres.Item.IsServicio) continue; // servicio no consume stock
                    int unidadesNecesarias = linea.CantidadPresentaciones * pres.Cantidad;
                    if (pres.Item.Stock < unidadesNecesarias)
                    {
                        insuficientes.Add($"{pres.Item.Nombre} — falta {unidadesNecesarias - pres.Item.Stock} unidad(es)");
                    }
                }

                if (insuficientes.Any())
                {
                    ModelState.AddModelError("", "No hay stock suficiente para: " + string.Join(", ", insuficientes));
                    await trx.RollbackAsync();
                    ViewBag.PreviousLineas = lineas;
                    await CargarViewBagsCrear();
                    return View();
                }

                // Preparar la venta
                var zonaGT = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var venta = new Venta
                {
                    NumeroVenta = await GenerarNumeroVentaAsync(),
                    Fecha = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaGT),
                    Id_Cliente = cliente?.Id_Cliente,
                    NombreConsumidor = string.IsNullOrWhiteSpace(nombreConsumidor) ? "Consumidor Final" : nombreConsumidor,
                    CreadoPor = User.Identity?.Name ?? "Sistema",
                    Id_Empleado = empleadoId,
                };

                decimal subtotal = 0m;
                decimal totalDescuentos = 0m;

                // Guardar venta primero para obtener Id_Venta
                _context.Ventas.Add(venta);
                await _context.SaveChangesAsync();

                // Procesar lineas
                foreach (var linea in lineas)
                {
                    var pres = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == linea.Id_ItemPresentacion);

                    if (pres == null) continue;

                    int unidades = linea.CantidadPresentaciones * pres.Cantidad;
                    decimal precioPorPresentacion = pres.PrecioVenta;
                    decimal descuento = linea.Descuento;
                    decimal subtotalLinea = Math.Round((precioPorPresentacion * linea.CantidadPresentaciones) - descuento, 2);

                    var detalle = new VentaDetalle
                    {
                        Id_Venta = venta.Id_Venta,
                        Id_Item = pres.Item.Id_Item,
                        Id_ItemPresentacion = pres.Id_ItemPresentacion,
                        CantidadPresentaciones = linea.CantidadPresentaciones,
                        CantidadUnidades = unidades,
                        PrecioVentaPorPresentacion = precioPorPresentacion,
                        Descuento = descuento,
                        Subtotal = subtotalLinea,
                        EsServicio = pres.Item.IsServicio
                    };

                    // Restar stock si no es servicio
                    if (!pres.Item.IsServicio)
                    {
                        pres.Item.Stock -= unidades;
                        _context.Items.Update(pres.Item);
                    }

                    subtotal += Math.Round(precioPorPresentacion * linea.CantidadPresentaciones, 2);
                    totalDescuentos += descuento;

                    _context.VentaDetalles.Add(detalle);
                }

                venta.Subtotal = subtotal;
                venta.Descuentos = totalDescuentos;
                venta.Total = subtotal - totalDescuentos;

                // Manejo de pago: vamos a crear un PagoVenta con info mínima
                // ventaInput.MontoRecibido y ventaInput.MetodoPago se pueden recibir desde el form
                venta.MetodoPago = ventaInput.MetodoPago;
                venta.MontoRecibido = ventaInput.MontoRecibido;
                venta.Cambio = ventaInput.MontoRecibido > 0 ? Math.Round(ventaInput.MontoRecibido - venta.Total, 2) : 0;

                _context.Ventas.Update(venta);

                var pago = new PagoVenta
                {
                    Id_Venta = venta.Id_Venta,
                    Metodo = venta.MetodoPago,
                    Monto = venta.Total,
                    Fecha = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaGT),
                    Nota = venta.MetodoPago == MetodoPagoVenta.Tarjeta ? "Pago con tarjeta" : null
                };

                _context.PagoVentas.Add(pago);

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Venta registrada correctamente.";
                return RedirectToAction(nameof(Details), new { id = venta.Id_Venta });
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                ModelState.AddModelError("", "Error al registrar la venta: " + ex.Message);
                TempData["ErrorMessage"] = "Ocurrió un error al registrar la venta. " + ex.Message;
                ViewBag.PreviousLineas = lineas;
                await CargarViewBagsCrear();
                return View();
            }
        }

        private async Task CargarViewBagsCrear()
        {
            var items = await _context.Items
                .Where(i => i.Activo)
                .Include(i => i.Presentaciones)
                    .ThenInclude(p => p.UnidadMedida)
                .Select(i => new
                {
                    id_Item = i.Id_Item,
                    nombre = i.Nombre,
                    codigo = i.Codigo,
                    isServicio = i.IsServicio,
                    presentaciones = i.Presentaciones.Where(p => p.Activo).Select(p => new {
                        id_ItemPresentacion = p.Id_ItemPresentacion,
                        cantidad = p.Cantidad,
                        precioVenta = p.PrecioVenta,
                        unidad = p.UnidadMedida.Nombre,
                        stock = i.Stock
                    })
                })
                .ToListAsync();

            ViewBag.Items = items;
            ViewBag.Clientes = await _context.Clientes.Where(c => c.Activo).ToListAsync();
        }

    }
}
