using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;
using QuestPDF.Fluent;
using PropamaPOS.Services;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ComprasController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;
        private readonly EmailServiceClient _emailClient;

        public ComprasController(AppDbContext context, IConfiguration config, EmailServiceClient emailClient)
        {
            _context = context;
            _config = config;
            _emailClient = emailClient;
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

        private async Task<string> GenerarNumeroCompraAsync()
        {
            int ultimoNumero = 0;
            var ultimaCompra = await _context.Compras
                .OrderByDescending(c => c.Id_Compra)
                .FirstOrDefaultAsync();

            if (ultimaCompra != null && !string.IsNullOrEmpty(ultimaCompra.NumeroCompra))
            {
                var parteNumerica = ultimaCompra.NumeroCompra.Replace("CMP-", "");
                int.TryParse(parteNumerica, out ultimoNumero);
            }
            int nuevoNumero = ultimoNumero + 1;
            return $"CMP-{nuevoNumero.ToString("D6")}";
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
                // obtener empleado si aplica
                int? empleadoId = null;
                var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(userIdClaim, out int idUsuario))
                {
                    var empleado = await _context.Empleados.FirstOrDefaultAsync(e => e.Id_Usuario == idUsuario);
                    if (empleado != null) empleadoId = empleado.Id_Empleado;
                }

                var zonaGT = TimeZoneInfo.FindSystemTimeZoneById("Central America Standard Time");
                var compra = new Compra
                {
                    NumeroCompra = await GenerarNumeroCompraAsync(),
                    Fecha = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, zonaGT),
                    Id_Proveedor = model.Id_Proveedor,
                    CreadoPor = User.Identity?.Name ?? "Administrador",
                    Nota = model.Nota,
                    Id_Empleado = empleadoId,
                    Estado = CompraEstado.Borrador // <-- guardar como borrador
                };

                _context.Compras.Add(compra);
                await _context.SaveChangesAsync();

                decimal totalWithIva = 0m;
                decimal subtotalWithoutIva = 0m;
                decimal ivaTotal = 0m;

                decimal ivaRate = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;

                foreach (var linea in model.Lineas)
                {
                    var presentacion = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == linea.Id_ItemPresentacion);

                    if (presentacion == null) continue;

                    decimal precioCostoPorPresentacionWithIva = linea.PrecioCostoPorPresentacion;
                    decimal precioCostoPorPresentacionSinIva = Math.Round(precioCostoPorPresentacionWithIva / (1 + ivaRate), 4);

                    var item = presentacion.Item;
                    int unidadesCompradas = linea.CantidadPresentaciones * presentacion.Cantidad;
                    decimal costoPorUnidad = Math.Round(precioCostoPorPresentacionSinIva / presentacion.Cantidad, 4);
                    decimal lineTotalWithIva = Math.Round(linea.PrecioCostoPorPresentacion * linea.CantidadPresentaciones, 2);
                    decimal lineSubtotalWithoutIva = Math.Round(lineTotalWithIva / (1 + ivaRate), 2);

                    var detalle = new CompraDetalle
                    {
                        Id_Compra = compra.Id_Compra,
                        Id_Item = item.Id_Item,
                        Id_ItemPresentacion = presentacion.Id_ItemPresentacion,
                        CantidadPresentaciones = linea.CantidadPresentaciones,
                        PrecioCostoPorPresentacion = linea.PrecioCostoPorPresentacion,
                        PrecioCostoPorUnidad = costoPorUnidad,
                        Subtotal = lineTotalWithIva
                    };

                    _context.CompraDetalles.Add(detalle);

                    totalWithIva += lineTotalWithIva;
                    subtotalWithoutIva += lineSubtotalWithoutIva;
                }

                ivaTotal = Math.Round(totalWithIva - subtotalWithoutIva, 2);

                compra.Subtotal = subtotalWithoutIva; 
                compra.IVA = ivaTotal;
                compra.Total = totalWithIva;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Compra registrada en estado Borrador.";
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

        // POST: Compras/MarcarPendiente/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarPendiente(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(c => c.Id_Compra == id);

            if (compra == null) return NotFound();

            if (compra.Estado != CompraEstado.Borrador)
            {
                TempData["ErrorMessage"] = "Solo las compras en estado Borrador pueden marcarse como Pendiente.";
                return RedirectToAction(nameof(Index));
            }

            compra.Estado = CompraEstado.Pendiente;
            _context.Compras.Update(compra);
            await _context.SaveChangesAsync();

            try
            {
                if (!string.IsNullOrEmpty(compra.Proveedor?.Correo))
                {
                    string nombreNegocio = "Librería y Papelería Propama";
                    string nombreProveedor = compra.Proveedor.Nombre ?? "Proveedor";
                    string correoProveedor = compra.Proveedor.Correo;
                    string fechaCompra = compra.Fecha.ToString("dd/MM/yyyy HH:mm");

                    // Construir tabla HTML con detalles
                    string tablaDetalles = "<table style='border-collapse:collapse;width:100%;border:1px solid #ddd;'>"
                                         + "<thead><tr style='background-color:#f2f2f2;text-align:left;'>"
                                         + "<th style='padding:8px;border:1px solid #ddd;'>Producto</th>"
                                         + "<th style='padding:8px;border:1px solid #ddd;'>Presentación</th>"
                                         + "<th style='padding:8px;border:1px solid #ddd;'>Cantidad</th>"
                                         + "<th style='padding:8px;border:1px solid #ddd;'>Costo (Q)</th>"
                                         + "<th style='padding:8px;border:1px solid #ddd;'>Subtotal (Q)</th>"
                                         + "</tr></thead><tbody>";

                    foreach (var det in compra.Detalles)
                    {
                        tablaDetalles += $"<tr>"
                            + $"<td style='padding:8px;border:1px solid #ddd;'>{det.Item?.Nombre ?? "-"}</td>"
                            + $"<td style='padding:8px;border:1px solid #ddd;'>{det.Presentacion?.UnidadMedida?.Nombre ?? "-"}</td>"
                            + $"<td style='padding:8px;border:1px solid #ddd;text-align:right;'>{det.CantidadPresentaciones}</td>"
                            + $"<td style='padding:8px;border:1px solid #ddd;text-align:right;'>Q{det.PrecioCostoPorPresentacion:F2}</td>"
                            + $"<td style='padding:8px;border:1px solid #ddd;text-align:right;'>Q{det.Subtotal:F2}</td>"
                            + $"</tr>";
                    }

                    tablaDetalles += "</tbody></table>";

                    // Construir cuerpo del correo
                    var subject = $"Orden de Compra {compra.NumeroCompra} - Confirmación";

                    var body = $@"
                    <table width='100%' cellpadding='0' cellspacing='0' style='background-color:#f5f7fa;padding:30px 0;font-family:Arial,Helvetica,sans-serif;'>
                      <tr>
                        <td align='center'>
                          <table width='700' cellpadding='0' cellspacing='0' style='background-color:#ffffff;border-radius:10px;overflow:hidden;box-shadow:0 0 10px rgba(0,0,0,0.05);'>
                            <tr>
                              <td style='background-color:#0066cc;color:white;text-align:center;padding:20px;font-size:22px;font-weight:bold;'>
                                Librería y Papelería Propama
                              </td>
                            </tr>
                            <tr>
                              <td style='padding:30px;font-size:15px;color:#333333;'>
                                <p><strong>{nombreProveedor}</strong>,</p>
                                <p>Reciba un cordial saludo. Desde <strong>{nombreNegocio}</strong> le informamos que hemos generado una nueva <strong>Orden de Compra</strong> con el fin de solicitar el suministro de los siguientes productos:</p>

                                <p>
                                  <strong>Número de Orden:</strong> {compra.NumeroCompra}<br/>
                                  <strong>Fecha de Emisión:</strong> {fechaCompra}<br/>
                                  <strong>Total:</strong> Q{compra.Total:F2}<br/>
                                  <strong>Estado:</strong> {compra.Estado}
                                </p>

                                <h3 style='margin-top:25px;border-bottom:2px solid #0066cc;padding-bottom:5px;'>Detalle de Productos Solicitados</h3>

                                <table width='100%' cellpadding='0' cellspacing='0' style='border-collapse:collapse;width:100%;margin-top:10px;'>
                                  <thead>
                                    <tr style='background-color:#f0f4ff;color:#333;border-bottom:2px solid #ddd;'>
                                      <th style='padding:10px;border:1px solid #ddd;text-align:left;'>Producto</th>
                                      <th style='padding:10px;border:1px solid #ddd;text-align:left;'>Presentación</th>
                                      <th style='padding:10px;border:1px solid #ddd;text-align:right;'>Cantidad</th>
                                      <th style='padding:10px;border:1px solid #ddd;text-align:right;'>Costo (Q)</th>
                                      <th style='padding:10px;border:1px solid #ddd;text-align:right;'>Subtotal (Q)</th>
                                    </tr>
                                  </thead>
                                  <tbody>";

                                        foreach (var det in compra.Detalles)
                                        {
                                            body += $@"
                                    <tr>
                                      <td style='padding:8px;border:1px solid #ddd;'>{det.Item?.Nombre ?? "-"}</td>
                                      <td style='padding:8px;border:1px solid #ddd;'>{det.Presentacion?.UnidadMedida?.Nombre ?? "-"}</td>
                                      <td style='padding:8px;border:1px solid #ddd;text-align:right;'>{det.CantidadPresentaciones}</td>
                                      <td style='padding:8px;border:1px solid #ddd;text-align:right;'>Q{det.PrecioCostoPorPresentacion:F2}</td>
                                      <td style='padding:8px;border:1px solid #ddd;text-align:right;'>Q{det.Subtotal:F2}</td>
                                    </tr>";
                                        }

                                        body += $@"
                                  </tbody>
                                </table>

                                <p style='text-align:right;margin-top:20px;'>
                                  <strong>Subtotal:</strong> Q{compra.Subtotal:F2}<br/>
                                  <strong>IVA:</strong> Q{compra.IVA:F2}<br/>
                                  <strong>Total:</strong> Q{compra.Total:F2}
                                </p>";

                                        if (!string.IsNullOrWhiteSpace(compra.Nota))
                                            body += $"<p><strong>Nota adicional:</strong> {compra.Nota}</p>";

                                        body += @"
                                <p style='margin-top:25px;'>Agradecemos su pronta atención a este pedido y quedamos atentos a la confirmación del despacho o entrega correspondiente.</p>
                                <p style='margin-top:25px;'>Atentamente,<br/><strong>Librería y Papelería Propama</strong></p>
                              </td>
                            </tr>
                            <tr>
                              <td style='background-color:#f0f0f0;text-align:center;padding:15px;font-size:12px;color:#555555;'>
                                Este es un correo automático, por favor no responder.<br/>
                                &copy; " + DateTime.Now.Year + @" Librería y Papelería Propama
                              </td>
                            </tr>
                          </table>
                        </td>
                      </tr>
                    </table>";


                    // Enviar correo
                    var (success, message) = await _emailClient.SendAsync(correoProveedor, subject, body);

                    if (success)
                        TempData["SuccessMessage"] = $"Compra {compra.NumeroCompra} marcada como Pendiente y correo enviado al proveedor.";
                    else
                        TempData["ErrorMessage"] = $"Compra marcada como Pendiente, pero no se pudo enviar el correo: {message}";
                }
                else
                {
                    TempData["ErrorMessage"] = "Compra marcada como Pendiente, pero el proveedor no tiene correo registrado.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Compra marcada como Pendiente, pero ocurrió un error al enviar el correo: {ex.Message}";
            }

            return RedirectToAction(nameof(Index));
        }



        // POST: Compras/Cancelar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var compra = await _context.Compras.FindAsync(id);
            if (compra == null) return NotFound();

            if (compra.Estado == CompraEstado.Recibida)
            {
                TempData["ErrorMessage"] = "No se puede cancelar una compra que ya fue recibida.";
                return RedirectToAction(nameof(Index));
            }

            compra.Estado = CompraEstado.Cancelada;
            _context.Compras.Update(compra);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = $"Compra {compra.NumeroCompra} cancelada.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Compras/MarcarRecibida/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarcarRecibida(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.Item)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(c => c.Id_Compra == id);

            if (compra == null) return NotFound();

            if (compra.Estado == CompraEstado.Recibida)
            {
                TempData["ErrorMessage"] = "La compra ya está en estado Recibida.";
                return RedirectToAction(nameof(Index));
            }

            if (compra.Estado == CompraEstado.Cancelada)
            {
                TempData["ErrorMessage"] = "No se puede recibir una compra cancelada.";
                return RedirectToAction(nameof(Index));
            }

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                decimal ivaRate = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;
                decimal markup = _config.GetValue<decimal?>("Pricing:DefaultMarkup") ?? 0.15m;
                decimal retailSurcharge = _config.GetValue<decimal?>("Pricing:RetailSurcharge") ?? 0.10m;

                foreach (var det in compra.Detalles)
                {
                    var presentacion = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                            .ThenInclude(i => i.Presentaciones)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == det.Id_ItemPresentacion);

                    if (presentacion == null) continue;

                    var item = presentacion.Item;

                    int unidadesCompradas = det.CantidadPresentaciones * presentacion.Cantidad;
                    decimal costoPorUnidad = det.PrecioCostoPorUnidad;

                    // Calcular nuevo costo promedio
                    int stockPrevio = item.Stock;
                    decimal costoPrevioTotal = stockPrevio * item.CostoPromedioUnidad;
                    decimal costoNuevoTotal = unidadesCompradas * costoPorUnidad;
                    int nuevoStock = stockPrevio + unidadesCompradas;
                    decimal nuevoCostoPromedio = nuevoStock == 0 ? costoPorUnidad :
                        Math.Round((costoPrevioTotal + costoNuevoTotal) / nuevoStock, 4);

                    // Actualizar stock y costo promedio del item
                    item.Stock = nuevoStock;
                    item.CostoPromedioUnidad = nuevoCostoPromedio;
                    _context.Items.Update(item);

                    // Propagar precios
                    var allPres = item.Presentaciones.Where(p => p.Activo).ToList();
                    foreach (var pres in allPres)
                    {
                        decimal costoPorPresentacion = Math.Round(costoPorUnidad * pres.Cantidad, 2);
                        pres.PrecioCosto = costoPorPresentacion;

                        decimal extra = pres.Cantidad == 1 ? retailSurcharge : 0m;
                        decimal markupForPresentation = markup + extra;

                        pres.PrecioVenta = Math.Round(costoPorPresentacion * (1 + markupForPresentation), 2);

                        _context.ItemPresentaciones.Update(pres);
                    }
                }

                compra.Estado = CompraEstado.Recibida;
                _context.Compras.Update(compra);
                await _context.SaveChangesAsync();

                await trx.CommitAsync();
                TempData["SuccessMessage"] = $"Compra {compra.NumeroCompra} marcada como Recibida y se aplicaron los cambios de inventario/precios.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                TempData["ErrorMessage"] = $"Error al marcar como Recibida: {ex.Message}";
                return RedirectToAction(nameof(Index));
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


        // GET: Compras/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var compra = await _context.Compras
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(c => c.Id_Compra == id);

            if (compra == null) return NotFound();
            if (compra.Estado != CompraEstado.Borrador)
            {
                TempData["ErrorMessage"] = "Solo las compras en estado Borrador pueden editarse.";
                return RedirectToAction(nameof(Index));
            }
            var model = new CompraCrearViewModel
            {
                Id_Proveedor = compra.Id_Proveedor,
                Fecha = compra.Fecha,
                Nota = compra.Nota,
                Lineas = compra.Detalles.Select(d => new Models.ViewModels.CompraLineaCrearViewModel
                {
                    Id_ItemPresentacion = d.Id_ItemPresentacion,
                    CantidadPresentaciones = d.CantidadPresentaciones,
                    PrecioCostoPorPresentacion = d.PrecioCostoPorPresentacion
                }).ToList()
            };

            ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();

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
            ViewBag.Id_Compra = compra.Id_Compra;

            return View("Editar", model); 
        }

        // POST: Compras/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, CompraCrearViewModel model)
        {
            var compra = await _context.Compras
                .Include(c => c.Detalles)
                .FirstOrDefaultAsync(c => c.Id_Compra == id);

            if (compra == null) return NotFound();
            if (compra.Estado != CompraEstado.Borrador)
            {
                TempData["ErrorMessage"] = "Solo las compras en estado Borrador pueden editarse.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                await CargarViewBags();
                return View("Crear", model);
            }

            using var trx = await _context.Database.BeginTransactionAsync();
            try
            {
                compra.Id_Proveedor = model.Id_Proveedor;
                compra.Nota = model.Nota;
                compra.Fecha = model.Fecha ?? compra.Fecha;

                var antiguos = await _context.CompraDetalles.Where(d => d.Id_Compra == compra.Id_Compra).ToListAsync();
                _context.CompraDetalles.RemoveRange(antiguos);

                decimal totalWithIva = 0m;
                decimal subtotalWithoutIva = 0m;
                decimal ivaRate = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;

                foreach (var linea in model.Lineas)
                {
                    var presentacion = await _context.ItemPresentaciones
                        .Include(p => p.Item)
                        .FirstOrDefaultAsync(p => p.Id_ItemPresentacion == linea.Id_ItemPresentacion);

                    if (presentacion == null) continue;

                    decimal lineTotalWithIva = Math.Round(linea.PrecioCostoPorPresentacion * linea.CantidadPresentaciones, 2);
                    decimal lineSubtotalWithoutIva = Math.Round(lineTotalWithIva / (1 + ivaRate), 2);
                    decimal precioCostoPorPresentacionSinIva = Math.Round(linea.PrecioCostoPorPresentacion / (1 + ivaRate), 4);
                    decimal costoPorUnidad = Math.Round(precioCostoPorPresentacionSinIva / presentacion.Cantidad, 4);

                    var detalle = new CompraDetalle
                    {
                        Id_Compra = compra.Id_Compra,
                        Id_Item = presentacion.Item.Id_Item,
                        Id_ItemPresentacion = presentacion.Id_ItemPresentacion,
                        CantidadPresentaciones = linea.CantidadPresentaciones,
                        PrecioCostoPorPresentacion = linea.PrecioCostoPorPresentacion,
                        PrecioCostoPorUnidad = costoPorUnidad,
                        Subtotal = lineTotalWithIva
                    };

                    _context.CompraDetalles.Add(detalle);

                    totalWithIva += lineTotalWithIva;
                    subtotalWithoutIva += lineSubtotalWithoutIva;
                }

                compra.Subtotal = subtotalWithoutIva;
                compra.IVA = Math.Round(totalWithIva - subtotalWithoutIva, 2);
                compra.Total = totalWithIva;

                await _context.SaveChangesAsync();
                await trx.CommitAsync();

                TempData["SuccessMessage"] = "Compra actualizada.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                await trx.RollbackAsync();
                ModelState.AddModelError("", $"Error al actualizar la compra: {ex.Message}");
                await CargarViewBags();
                return View("Crear", model);
            }
        }


        [HttpGet]
        public async Task<IActionResult> DownloadPdf(int id)
        {
            decimal ivaRate = _config.GetValue<decimal?>("Tax:IVA") ?? 0.12m;

            var compra = await _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .FirstOrDefaultAsync(c => c.Id_Compra == id);

            if (compra == null) return NotFound();

            QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

            // Datos del negocio
            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            // Datos del proveedor
            string nombreProveedor = compra.Proveedor?.Nombre ?? "Proveedor no registrado";

            var pdfBytes = QuestPDF.Fluent.Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(40);
                    page.DefaultTextStyle(x => x.FontSize(11));
                    page.Size(QuestPDF.Helpers.PageSizes.A4);

                    // Encabezado
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
                                col.Item().Text($"Orden de Compra: {compra.NumeroCompra}").Bold();
                                col.Item().Text($"Fecha: {compra.Fecha:dd/MM/yyyy HH:mm}");
                                col.Item().Text($"Estado: {compra.Estado}");
                                col.Item().Text($"Creado por: {compra.CreadoPor}");
                            });

                            row.RelativeItem().Column(col =>
                            {
                                col.Item().Text($"Proveedor: {nombreProveedor}");
                            });
                        });
                    });

                    // Contenido principal
                    page.Content().Column(col =>
                    {
                        col.Item().PaddingVertical(10).Text("Detalle de Productos").FontSize(13).Bold();
                        col.Item().Table(table =>
                        {
                            table.ColumnsDefinition(c =>
                            {
                                c.RelativeColumn(3); // Producto
                                c.RelativeColumn(2); // Presentación
                                c.RelativeColumn(1); // Cantidad
                                c.RelativeColumn(1.5f); // Precio costo
                                c.RelativeColumn(1.2f); // Subtotal
                            });

                            // Encabezado
                            table.Header(h =>
                            {
                                h.Cell().Background("#eeeeee").Padding(5).Text("Producto").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).Text("Presentación").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Cant.").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Costo (Q)").Bold();
                                h.Cell().Background("#eeeeee").Padding(5).AlignRight().Text("Subtotal (Q)").Bold();
                            });

                            foreach (var det in compra.Detalles)
                            {
                                table.Cell().Padding(4).Text(det.Item?.Nombre ?? "-");
                                table.Cell().Padding(4).Text(det.Presentacion?.UnidadMedida?.Nombre ?? "-");
                                table.Cell().Padding(4).AlignRight().Text(det.CantidadPresentaciones.ToString());
                                table.Cell().Padding(4).AlignRight().Text($"Q{det.PrecioCostoPorPresentacion:F2}");
                                table.Cell().Padding(4).AlignRight().Text($"Q{det.Subtotal:F2}");
                            }
                        });

                        // Totales
                        col.Item().PaddingTop(10).AlignRight().Column(total =>
                        {
                            total.Item().Text($"Subtotal: Q{compra.Subtotal:F2}");
                            total.Item().Text($"IVA: Q{compra.IVA:F2}");
                            total.Item().Text($"Total: Q{compra.Total:F2}").Bold().FontSize(13);
                        });

                        if (!string.IsNullOrWhiteSpace(compra.Nota))
                        {
                            col.Item().PaddingTop(10).Text($"Nota: {compra.Nota}");
                        }
                    });

                    // Pie de página
                    page.Footer().Column(footer =>
                    {
                        footer.Item().PaddingVertical(5).LineHorizontal(1);
                        footer.Item().AlignCenter().Text("Documento generado por PropamaPOS").FontSize(10).Italic();
                    });
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Compra_{compra.NumeroCompra}.pdf");
        }

    }
}
