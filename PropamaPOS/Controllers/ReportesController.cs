using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using Microsoft.Extensions.Configuration;
using QuestPDF.Fluent;
using QuestPDF.Infrastructure;
using QuestPDF.Helpers;
using System.Globalization;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")] 
    public class ReportesController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IConfiguration _config;

        public ReportesController(AppDbContext context, IConfiguration config)
        {
            _context = context;
            _config = config;
        }

        // GET: Reportes/Index
        public async Task<IActionResult> Index(
        string proveedor = null,
        DateTime? desde = null,
        DateTime? hasta = null,
        string numero = null,
        string empleado = null,
        int page = 1)
        {
            const int PageSize = 30;

            var query = _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Empleado)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .AsQueryable();

            // 🔹 Solo mostrar las compras con estado "Recibida"
            query = query.Where(c => c.Estado == CompraEstado.Recibida);

            // --- Filtros ---
            if (!string.IsNullOrWhiteSpace(proveedor))
            {
                proveedor = proveedor.Trim().ToLower();
                query = query.Where(c =>
                    c.Proveedor != null &&
                    c.Proveedor.Nombre.ToLower().Contains(proveedor)
                );
            }

            if (desde.HasValue)
                query = query.Where(c => c.Fecha.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(c => c.Fecha.Date <= hasta.Value.Date);

            if (!string.IsNullOrWhiteSpace(numero))
            {
                numero = numero.Trim();
                query = query.Where(c => c.NumeroCompra.Contains(numero));
            }

            if (!string.IsNullOrWhiteSpace(empleado))
            {
                empleado = empleado.Trim().ToLower();
                query = query.Where(c =>
                    (c.Empleado != null && (
                        c.Empleado.Nombre.ToLower().Contains(empleado) ||
                        (c.Empleado.Apellido != null && c.Empleado.Apellido.ToLower().Contains(empleado))
                    ))
                    || c.CreadoPor.ToLower().Contains(empleado)
                );
            }

            // --- Paginación ---
            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var compras = await query
                .OrderByDescending(c => c.Fecha)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // --- ViewBag ---
            ViewBag.Proveedores = await _context.Proveedores.OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.CurrentProveedor = proveedor;
            ViewBag.CurrentDesde = desde?.ToString("yyyy-MM-dd");
            ViewBag.CurrentHasta = hasta?.ToString("yyyy-MM-dd");
            ViewBag.CurrentNumero = numero;
            ViewBag.CurrentEmpleado = empleado;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;

            return View(compras);
        }

        // GET: Reportes/DownloadComprasPdf
        public async Task<IActionResult> DownloadComprasPdf(
            string proveedor = null,
            DateTime? desde = null,
            DateTime? hasta = null,
            string numero = null,
            string empleado = null,
            int page = 1)
        {
            var query = _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Empleado)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .AsQueryable();

            query = query.Where(c => c.Estado == CompraEstado.Recibida);

            if (!string.IsNullOrWhiteSpace(proveedor))
            {
                proveedor = proveedor.Trim().ToLower();
                query = query.Where(c =>
                    c.Proveedor != null &&
                    c.Proveedor.Nombre.ToLower().Contains(proveedor)
                );
            }

            if (desde.HasValue)
                query = query.Where(c => c.Fecha.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(c => c.Fecha.Date <= hasta.Value.Date);

            if (!string.IsNullOrWhiteSpace(numero))
                query = query.Where(c => c.NumeroCompra.Contains(numero));

            if (!string.IsNullOrWhiteSpace(empleado))
            {
                empleado = empleado.Trim().ToLower();
                query = query.Where(c =>
                    (c.Empleado != null && (
                        c.Empleado.Nombre.ToLower().Contains(empleado) ||
                        (c.Empleado.Apellido != null && c.Empleado.Apellido.ToLower().Contains(empleado))
                    ))
                    || c.CreadoPor.ToLower().Contains(empleado)
                );
            }

            var compras = await query.OrderByDescending(c => c.Fecha).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(35);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    // ENCABEZADO
                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text(nombreNegocio).FontSize(16).Bold();
                        header.Item().AlignCenter().Text($"NIT: {nitNegocio}");
                        header.Item().AlignCenter().Text(direccionNegocio);
                        header.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                        header.Item().AlignCenter().Text("Reporte de Compras").FontSize(12).Bold();
                        string periodo = (desde.HasValue || hasta.HasValue)
                            ? $"Periodo: {(desde?.ToString("dd/MM/yyyy") ?? "Inicio")} - {(hasta?.ToString("dd/MM/yyyy") ?? "Fin")}"
                            : "Periodo: Todos";
                        header.Item().AlignCenter().Text(periodo).FontSize(9).Italic();
                        header.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                    });

                    // CONTENIDO
                    page.Content().PaddingVertical(10).Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Fecha
                                columns.RelativeColumn(2);    // Número
                                columns.RelativeColumn(3);    // Proveedor
                                columns.RelativeColumn(3);    // Empleado
                                columns.RelativeColumn(1);    // Líneas
                                columns.RelativeColumn(2);    // Total
                            });

                            // CABECERA
                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(HeaderCell).Text("Fecha");
                                headerRow.Cell().Element(HeaderCell).Text("Número");
                                headerRow.Cell().Element(HeaderCell).Text("Proveedor");
                                headerRow.Cell().Element(HeaderCell).Text("Empleado");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Líneas");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Total");
                            });

                            decimal totalTotal = 0m;
                            int rowIndex = 0;

                            foreach (var c in compras)
                            {
                                string empleadoNombre = c.Empleado != null
                                    ? $"{c.Empleado.Nombre} {c.Empleado.Apellido}"
                                    : c.CreadoPor;

                                var backgroundColor = rowIndex++ % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                                table.Cell().Element(r => CellStyle(r, backgroundColor)).Text(c.Fecha.ToLocalTime().ToString("dd/MM/yyyy"));
                                table.Cell().Element(r => CellStyle(r, backgroundColor)).Text(c.NumeroCompra);
                                table.Cell().Element(r => CellStyle(r, backgroundColor)).Text(c.Proveedor?.Nombre ?? "N/A");
                                table.Cell().Element(r => CellStyle(r, backgroundColor)).Text(empleadoNombre);
                                table.Cell().Element(r => CellStyle(r, backgroundColor)).AlignRight().Text((c.Detalles?.Count ?? 0).ToString());
                                table.Cell().Element(r => CellStyle(r, backgroundColor)).AlignRight().Text($"Q{c.Total:F2}");

                                totalTotal += c.Total;
                            }

                            // PIE
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(5).Element(TotalCell).AlignRight().Text("Total General:");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalTotal:F2}");
                            });

                            // Estilos
                            IContainer HeaderCell(IContainer c2) => c2
                                .Background(Colors.Grey.Darken2)
                                .PaddingVertical(5)
                                .PaddingHorizontal(3)
                                .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold())
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Darken2);

                            IContainer CellStyle(IContainer c2, string bg) => c2
                                .Background(bg)
                                .Padding(4)
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2);

                            IContainer TotalCell(IContainer c2) => c2
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .DefaultTextStyle(x => x.SemiBold().FontSize(11));
                        });
                    });

                    // PIE DE PÁGINA
                    page.Footer().AlignCenter().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Compras_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }


        // GET: Reportes/IndexVentas
        public async Task<IActionResult> IndexVentas(
            string numero,
            string cliente,
            string empleado,
            MetodoPagoVenta? metodoPago,
            DateTime? fechaDesde,
            DateTime? fechaHasta,
            int page = 1)
        {
            const int PageSize = 30;

            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Item)
                .AsQueryable();

            // --- Filtros ---

            if (!string.IsNullOrWhiteSpace(numero))
            {
                numero = numero.Trim().ToLower();
                query = query.Where(v => v.NumeroVenta.ToLower().Contains(numero));
            }

            if (!string.IsNullOrWhiteSpace(cliente))
            {
                cliente = cliente.Trim().ToLower();
                query = query.Where(v =>
                    (v.Cliente != null && (
                        v.Cliente.Nombre.ToLower().Contains(cliente) ||
                        (v.Cliente.Apellido != null && v.Cliente.Apellido.ToLower().Contains(cliente)) ||
                        (v.Cliente.NIT != null && v.Cliente.NIT.ToLower().Contains(cliente))
                    )) ||
                    (v.Cliente == null && v.NombreConsumidor.ToLower().Contains(cliente))
                );
            }

            if (!string.IsNullOrWhiteSpace(empleado))
            {
                empleado = empleado.Trim().ToLower();
                query = query.Where(v =>
                    (v.Empleado != null && (
                        v.Empleado.Nombre.ToLower().Contains(empleado) ||
                        (v.Empleado.Apellido != null && v.Empleado.Apellido.ToLower().Contains(empleado))
                    )) ||
                    v.CreadoPor.ToLower().Contains(empleado)
                );
            }

            if (metodoPago.HasValue)
                query = query.Where(v => v.MetodoPago == metodoPago.Value);

            if (fechaDesde.HasValue)
                query = query.Where(v => v.Fecha.Date >= fechaDesde.Value.Date);

            if (fechaHasta.HasValue)
                query = query.Where(v => v.Fecha.Date <= fechaHasta.Value.Date);

            // --- Orden y paginación ---
            query = query.OrderByDescending(v => v.Fecha);

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var ventas = await query
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            // --- ViewBag para filtros ---
            ViewBag.CurrentNumero = numero;
            ViewBag.CurrentCliente = cliente;
            ViewBag.CurrentEmpleado = empleado;
            ViewBag.CurrentMetodoPago = metodoPago;
            ViewBag.CurrentFechaDesde = fechaDesde?.ToString("yyyy-MM-dd");
            ViewBag.CurrentFechaHasta = fechaHasta?.ToString("yyyy-MM-dd");
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;

            return View("Ventas", ventas);
        }


        // GET: Reportes/DownloadVentasPdf
        public async Task<IActionResult> DownloadVentasPdf(
            string numero,
            string cliente,
            string empleado,
            MetodoPagoVenta? metodoPago,
            DateTime? fechaDesde,
            DateTime? fechaHasta)
        {
            var query = _context.Ventas
                .Include(v => v.Cliente)
                .Include(v => v.Empleado)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(v => v.Detalles)
                    .ThenInclude(d => d.Item)
                .AsQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(numero))
                query = query.Where(v => v.NumeroVenta.ToLower().Contains(numero.Trim().ToLower()));

            if (!string.IsNullOrWhiteSpace(cliente))
            {
                cliente = cliente.Trim().ToLower();
                query = query.Where(v =>
                    (v.Cliente != null && (
                        v.Cliente.Nombre.ToLower().Contains(cliente) ||
                        (v.Cliente.Apellido != null && v.Cliente.Apellido.ToLower().Contains(cliente)) ||
                        (v.Cliente.NIT != null && v.Cliente.NIT.ToLower().Contains(cliente))
                    )) ||
                    (v.Cliente == null && v.NombreConsumidor.ToLower().Contains(cliente))
                );
            }

            if (!string.IsNullOrWhiteSpace(empleado))
            {
                empleado = empleado.Trim().ToLower();
                query = query.Where(v =>
                    (v.Empleado != null && (
                        v.Empleado.Nombre.ToLower().Contains(empleado) ||
                        (v.Empleado.Apellido != null && v.Empleado.Apellido.ToLower().Contains(empleado))
                    )) ||
                    v.CreadoPor.ToLower().Contains(empleado)
                );
            }

            if (metodoPago.HasValue)
                query = query.Where(v => v.MetodoPago == metodoPago.Value);

            if (fechaDesde.HasValue)
                query = query.Where(v => v.Fecha.Date >= fechaDesde.Value.Date);

            if (fechaHasta.HasValue)
                query = query.Where(v => v.Fecha.Date <= fechaHasta.Value.Date);

            var ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(35);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text(nombreNegocio).FontSize(16).Bold();
                        header.Item().AlignCenter().Text($"NIT: {nitNegocio}");
                        header.Item().AlignCenter().Text(direccionNegocio);
                        header.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                        header.Item().AlignCenter().Text("Reporte de Ventas").FontSize(12).Bold();
                        string periodo = (fechaDesde.HasValue || fechaHasta.HasValue)
                            ? $"Periodo: {(fechaDesde?.ToString("dd/MM/yyyy") ?? "Inicio")} - {(fechaHasta?.ToString("dd/MM/yyyy") ?? "Fin")}"
                            : "Periodo: Todos";
                        header.Item().AlignCenter().Text(periodo).FontSize(9).Italic();
                        header.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                    });

                    page.Content().PaddingVertical(10).Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(2.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                                columns.RelativeColumn(1.5f);
                            });

                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(HeaderCell).Text("Fecha");
                                headerRow.Cell().Element(HeaderCell).Text("Número");
                                headerRow.Cell().Element(HeaderCell).Text("Cliente");
                                headerRow.Cell().Element(HeaderCell).Text("Empleado");
                                headerRow.Cell().Element(HeaderCell).Text("Pago");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Líneas");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Subtotal");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("IVA");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Total");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Utilidad");
                            });

                            decimal totalSubtotal = 0m, totalIva = 0m, totalTotal = 0m, totalUtilidad = 0m;
                            int rowIndex = 0;

                            foreach (var v in ventas)
                            {
                                string clienteNombre = v.Cliente != null ? $"{v.Cliente.Nombre} {v.Cliente.Apellido}" : v.NombreConsumidor ?? "Consumidor Final";
                                string empleadoNombre = v.Empleado != null ? $"{v.Empleado.Nombre} {v.Empleado.Apellido}" : v.CreadoPor ?? "N/A";
                                string metodo = v.MetodoPago.ToString();
                                var bg = rowIndex++ % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                                decimal utilidad = 0m;
                                foreach (var d in v.Detalles)
                                {
                                    decimal costo = d.Presentacion?.PrecioCosto ?? 0m;
                                    utilidad += (d.PrecioVentaPorPresentacion - costo) * d.CantidadPresentaciones;
                                }

                                table.Cell().Element(r => CellStyle(r, bg)).Text(v.Fecha.ToLocalTime().ToString("dd/MM/yyyy"));
                                table.Cell().Element(r => CellStyle(r, bg)).Text(v.NumeroVenta ?? "-");
                                table.Cell().Element(r => CellStyle(r, bg)).Text(clienteNombre);
                                table.Cell().Element(r => CellStyle(r, bg)).Text(empleadoNombre);
                                table.Cell().Element(r => CellStyle(r, bg)).Text(metodo);
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text((v.Detalles?.Count ?? 0).ToString());
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{v.Subtotal:F2}");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{v.IVA:F2}");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{v.Total:F2}");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{utilidad:F2}");

                                totalSubtotal += v.Subtotal;
                                totalIva += v.IVA;
                                totalTotal += v.Total;
                                totalUtilidad += utilidad;
                            }

                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(6).Element(TotalCell).AlignRight().Text("Totales:");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalSubtotal:F2}");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalIva:F2}");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalTotal:F2}");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalUtilidad:F2}");
                            });

                            // estilos
                            IContainer HeaderCell(IContainer c2) => c2
                                .Background(Colors.Grey.Darken2)
                                .PaddingVertical(5)
                                .PaddingHorizontal(3)
                                .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold())
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Darken2);

                            IContainer CellStyle(IContainer c2, string bg) => c2
                                .Background(bg)
                                .Padding(4)
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2);

                            IContainer TotalCell(IContainer c2) => c2
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .DefaultTextStyle(x => x.SemiBold().FontSize(11));
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }



        // GET: Reportes/IndexInventario
        public async Task<IActionResult> IndexInventario(int? categoriaId, bool? bajoStock, string q = null, int page = 1)
        {
            const int PageSize = 30;

            var query = _context.Items
                .Where(i => i.Activo && !i.IsServicio)
                .Include(i => i.Categoria)
                .Include(i => i.Presentaciones.Where(p => p.Activo))
                    .ThenInclude(p => p.UnidadMedida)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(i => i.Id_Categoria == categoriaId.Value);

            if (bajoStock.HasValue && bajoStock.Value)
                query = query.Where(i => i.Stock <= (i.StockMinimo ?? 0));

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(i => i.Nombre.Contains(q) || i.Codigo.Contains(q));
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var items = await query
                .OrderBy(i => i.Nombre)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.CurrentCategoria = categoriaId;
            ViewBag.CurrentBajoStock = bajoStock;
            ViewBag.CurrentQ = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalItems = total;

            return View("Inventario", items);
        }


        // GET: Reportes/DownloadInventarioPdf
        public async Task<IActionResult> DownloadInventarioPdf(int? categoriaId, bool? bajoStock, string q = null)
        {
            var query = _context.Items
                .Where(i => i.Activo && !i.IsServicio)
                .Include(i => i.Categoria)
                .Include(i => i.Presentaciones.Where(p => p.Activo))
                    .ThenInclude(p => p.UnidadMedida)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(i => i.Id_Categoria == categoriaId.Value);

            if (bajoStock.HasValue && bajoStock.Value)
                query = query.Where(i => i.Stock <= (i.StockMinimo ?? 0));

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(i => i.Nombre.Contains(q) || i.Codigo.Contains(q));
            }

            var items = await query.OrderBy(i => i.Nombre).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(35);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Encabezado
                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text(nombreNegocio).FontSize(16).Bold();
                        header.Item().AlignCenter().Text($"NIT: {nitNegocio}");
                        header.Item().AlignCenter().Text(direccionNegocio);
                        header.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                        header.Item().AlignCenter().Text("Reporte de Inventario").FontSize(12).Bold();
                        string filtroTxt = bajoStock == true ? "Solo bajo stock" : "Todos los productos";
                        header.Item().AlignCenter().Text($"Filtro: {filtroTxt}").FontSize(9).Italic();
                        header.Item().PaddingVertical(4).LineHorizontal(1).LineColor(Colors.Grey.Darken2);
                    });

                    // Contenido
                    page.Content().PaddingVertical(10).Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Producto
                                columns.RelativeColumn(2); // Categoría
                                columns.RelativeColumn(1); // Stock
                                columns.RelativeColumn(1.2f); // Stock min
                                columns.RelativeColumn(1.5f); // Costo
                                columns.RelativeColumn(1.5f); // Precio
                                columns.RelativeColumn(1.8f); // Valor inventario
                                columns.RelativeColumn(1.8f); // Utilidad
                            });

                            // Encabezado
                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(HeaderCell).Text("Producto");
                                headerRow.Cell().Element(HeaderCell).Text("Categoría");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Stock");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Stock Min");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Costo (Q)");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Precio (Q)");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Valor Inv. (Q)");
                                headerRow.Cell().Element(HeaderCell).AlignRight().Text("Utilidad Pot. (Q)");
                            });

                            decimal totalValorInv = 0m, totalUtilidad = 0m;
                            int rowIndex = 0;

                            foreach (var i in items)
                            {
                                var costo = i.CostoPromedioUnidad;
                                var pres = i.Presentaciones?.FirstOrDefault();
                                var precio = pres?.PrecioVenta ?? 0m;

                                var valorInv = Math.Round(i.Stock * precio, 2);
                                var utilidadPot = Math.Round((precio - costo) * i.Stock, 2);

                                totalValorInv += valorInv;
                                totalUtilidad += utilidadPot;

                                var bg = rowIndex++ % 2 == 0 ? Colors.Grey.Lighten5 : Colors.White;

                                table.Cell().Element(r => CellStyle(r, bg)).Text(i.Nombre);
                                table.Cell().Element(r => CellStyle(r, bg)).Text(i.Categoria?.Nombre ?? "-");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text(i.Stock.ToString());
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text((i.StockMinimo ?? 0).ToString());
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{costo:F2}");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{precio:F2}");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{valorInv:F2}");
                                table.Cell().Element(r => CellStyle(r, bg)).AlignRight().Text($"Q{utilidadPot:F2}");
                            }

                            // Totales
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(6).Element(TotalCell).AlignRight().Text("Totales:");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalValorInv:F2}");
                                footer.Cell().Element(TotalCell).AlignRight().Text($"Q{totalUtilidad:F2}");
                            });

                            // --- Estilos ---
                            IContainer HeaderCell(IContainer c2) => c2
                                .Background(Colors.Grey.Darken2)
                                .PaddingVertical(5)
                                .PaddingHorizontal(3)
                                .DefaultTextStyle(x => x.FontColor(Colors.White).SemiBold())
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Darken2);

                            IContainer CellStyle(IContainer c2, string bg) => c2
                                .Background(bg)
                                .Padding(4)
                                .BorderBottom(0.5f)
                                .BorderColor(Colors.Grey.Lighten2);

                            IContainer TotalCell(IContainer c2) => c2
                                .Background(Colors.Grey.Lighten3)
                                .Padding(5)
                                .Border(0.5f)
                                .BorderColor(Colors.Grey.Lighten2)
                                .DefaultTextStyle(x => x.SemiBold().FontSize(11));
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generado: {DateTime.Now:dd/MM/yyyy HH:mm}").FontSize(8).Italic();
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Inventario_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }


    }
}