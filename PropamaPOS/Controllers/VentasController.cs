// PropamaPOS/Controllers/VentasController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;
using System.Security.Claims;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class VentasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public VentasController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
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

            if (facturarCon == "NIT" && string.IsNullOrWhiteSpace(nitInput))
            {
                ModelState.AddModelError("NIT", "Debe ingresar un NIT cuando el tipo de factura es 'Con NIT'.");
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
                                Direccion = ventaInput.Cliente?.Direccion ?? "Ciudad",
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

                decimal ivaRate = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;
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

                    // Restar stock o descontar insumos según el tipo de item
                    if (!pres.Item.IsServicio)
                    {
                        // PRODUCTO NORMAL: descuenta stock directo
                        pres.Item.Stock -= unidades;
                        _context.Items.Update(pres.Item);
                    }
                    else
                    {
                        // SERVICIO: obtener los componentes y descontar sus insumos
                        var componentes = await _context.ServicioComponentes
                            .Include(sc => sc.ItemConsumido)
                            .Where(sc => sc.Id_Servicio == pres.Item.Id_Item)
                            .ToListAsync();

                        foreach (var comp in componentes)
                        {
                            var insumo = comp.ItemConsumido;
                            if (insumo == null) continue;

                            // Calcular cuánto stock del insumo se consume en total
                            var totalConsumido = comp.CantidadPorServicio * unidades;

                            // Si trabajas con enteros de stock, redondea hacia arriba:
                            int totalConsumidoInt = (int)Math.Ceiling(totalConsumido);

                            // Validar stock suficiente
                            if (insumo.Stock < totalConsumidoInt)
                            {
                                ModelState.AddModelError("", $"Stock insuficiente del insumo '{insumo.Nombre}' para el servicio '{pres.Item.Nombre}'. " +
                                    $"Necesario: {totalConsumidoInt}, Disponible: {insumo.Stock}");
                                await trx.RollbackAsync();
                                ViewBag.PreviousLineas = lineas;
                                await CargarViewBagsCrear();
                                return View();
                            }

                            // Descontar del inventario
                            insumo.Stock -= totalConsumidoInt;
                            _context.Items.Update(insumo);
                        }
                    }

                    subtotal += Math.Round(precioPorPresentacion * linea.CantidadPresentaciones, 2);
                    totalDescuentos += descuento;

                    _context.VentaDetalles.Add(detalle);
                }

                venta.Subtotal = subtotal;
                venta.Descuentos = totalDescuentos;

                decimal imponible = Math.Round(subtotal - totalDescuentos, 2);
                decimal ivaTotal = Math.Round(imponible * ivaRate, 2);
                decimal totalConIva = Math.Round(imponible + ivaTotal, 2);

                venta.IVA = ivaTotal;
                venta.Total = totalConIva;
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

        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            decimal ivaRate = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;

            var venta = await _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(v => v.Id_Venta == id);

            if (venta == null) return NotFound();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Encabezado
            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            // Datos del cliente
            string nitReceptor = venta.Cliente?.NIT ?? "CF";
            string nombreReceptor = venta.Cliente != null
                ? $"{venta.Cliente.Nombre} {venta.Cliente.Apellido}"
                : venta.NombreConsumidor ?? "Consumidor Final";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Size(PageSizes.A4);

                    // ENCABEZADO
                    page.Header().Column(header =>
                    {
                        header.Item().Text(nombreNegocio).FontSize(18).Bold().AlignCenter();
                        header.Item().Text($"NIT Emisor: {nitNegocio}").AlignCenter();
                        header.Item().Text($"Dirección Emisor: {direccionNegocio}").AlignCenter();
                        header.Item().PaddingVertical(5).LineHorizontal(1);

                        header.Item().Row(row =>
                        {
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Factura: {venta.NumeroVenta}").Bold();
                                col.Item().Text($"Fecha: {venta.Fecha:dd/MM/yyyy HH:mm}");
                                col.Item().Text($"Empleado: {(venta.Empleado != null ? venta.Empleado.Nombre : "No registrado")}");
                            });
                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"NIT Receptor: {nitReceptor}");
                                col.Item().Text($"Nombre Receptor: {nombreReceptor}");
                                col.Item().Text($"Método de Pago: {venta.MetodoPago}");
                            });
                        });
                    });

                    // CONTENIDO PRINCIPAL
                    page.Content().Column(col =>
                    {
                        col.Item().PaddingVertical(10).Text("Detalle de Productos y Servicios").FontSize(13).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); // producto
                                c.RelativeColumn(2); // presentación
                                c.RelativeColumn(1); // cantidad
                                c.RelativeColumn(1); // precio
                                c.RelativeColumn(1); // descuento
                                c.RelativeColumn(1.2f); // subtotal
                            });

                            // encabezado tabla
                            table.Header(h =>
                            {
                                h.Cell().Background("#eeeeee").Padding(5).Text("Producto / Servicio").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).Text("Presentación").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Cant.").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Precio (Q)").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Desc. (Q)").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Subtotal (Q)").Bold();
                            });

                            foreach (var det in venta.Detalles)
                            {
                                table.Cell().Padding(4).Text(det.Item?.Nombre ?? "-");
                                table.Cell().Padding(4).Text(det.Presentacion?.UnidadMedida?.Nombre ?? "-");
                                table.Cell().Padding(4).AlignRight().Text(det.CantidadPresentaciones.ToString());
                                table.Cell().Padding(4).AlignRight().Text($"Q{(det.PrecioVentaPorPresentacion * (1 + ivaRate)):F2}");
                                table.Cell().Padding(4).AlignRight().Text($"Q{det.Descuento:F2}");
                                table.Cell().Padding(4).AlignRight().Text($"Q{det.Subtotal:F2}");
                            }
                        });

                        // Totales
                        col.Item().PaddingTop(10).AlignRight().Column(total =>
                        {
                            total.Item().Text($"Subtotal: Q{venta.Subtotal:F2}");
                            total.Item().Text($"Descuentos: Q{venta.Descuentos:F2}");
                            total.Item().Text($"IVA: Q{venta.IVA:F2}");
                            total.Item().Text($"Total: Q{venta.Total:F2}").Bold().FontSize(13);
                        });

                    });

                    // PIE DE PÁGINA
                    page.Footer().Column(footer =>
                    {
                        footer.Item().PaddingVertical(5).LineHorizontal(1);
                        footer.Item().AlignCenter().Text("Gracias por su compra — Librería y Papelería Propama")
                            .FontSize(10).Italic();
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Factura_{venta.NumeroVenta}.pdf");
        }

    }
}
