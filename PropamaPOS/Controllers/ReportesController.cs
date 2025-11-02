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

        // GET: Reportes/Index?Id_Proveedor=1&desde=2025-01-01&hasta=2025-01-31
        public async Task<IActionResult> Index(int? id_Proveedor, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .AsQueryable();

            if (id_Proveedor.HasValue)
                query = query.Where(c => c.Id_Proveedor == id_Proveedor.Value);

            if (desde.HasValue)
                query = query.Where(c => c.Fecha.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(c => c.Fecha.Date <= hasta.Value.Date);

            var compras = await query.OrderByDescending(c => c.Fecha).ToListAsync();

            ViewBag.Proveedores = await _context.Proveedores.Where(p => p.Activo).OrderBy(p => p.Nombre).ToListAsync();
            ViewBag.CurrentProveedor = id_Proveedor;
            ViewBag.CurrentDesde = desde?.ToString("yyyy-MM-dd");
            ViewBag.CurrentHasta = hasta?.ToString("yyyy-MM-dd");

            return View(compras);
        }

        // GET: Reportes/DownloadComprasPdf?Id_Proveedor=1&desde=2025-01-01&hasta=2025-01-31
        public async Task<IActionResult> DownloadComprasPdf(int? id_Proveedor, DateTime? desde, DateTime? hasta)
        {
            var query = _context.Compras
                .Include(c => c.Proveedor)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Presentacion)
                        .ThenInclude(p => p.UnidadMedida)
                .Include(c => c.Detalles)
                    .ThenInclude(d => d.Item)
                .AsQueryable();

            if (id_Proveedor.HasValue)
                query = query.Where(c => c.Id_Proveedor == id_Proveedor.Value);

            if (desde.HasValue)
                query = query.Where(c => c.Fecha.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(c => c.Fecha.Date <= hasta.Value.Date);

            var compras = await query.OrderByDescending(c => c.Fecha).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            // Información del negocio (ajusta si tienes variables reales)
            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text(nombreNegocio).FontSize(14).Bold();
                        header.Item().AlignCenter().Text($"NIT: {nitNegocio}");
                        header.Item().AlignCenter().Text(direccionNegocio);
                        header.Item().PaddingVertical(6).LineHorizontal(1);
                        header.Item().Text($"Reporte de Compras").FontSize(12).Bold().AlignCenter();
                        string periodo = (desde.HasValue || hasta.HasValue)
                            ? $"Periodo: {(desde?.ToString("dd/MM/yyyy") ?? "Inicio")} - {(hasta?.ToString("dd/MM/yyyy") ?? "Fin")}"
                            : $"Periodo: Todos";
                        header.Item().AlignCenter().Text(periodo).FontSize(9);
                    });

                    page.Content().PaddingVertical(6).Column(content =>
                    {
                        // Tabla resumen de compras
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Fecha
                                columns.RelativeColumn(2); // Número
                                columns.RelativeColumn(3); // Proveedor
                                columns.RelativeColumn(1); // Líneas
                                columns.RelativeColumn(2); // Subtotal
                                columns.RelativeColumn(2); // IVA
                                columns.RelativeColumn(2); // Total
                            });

                            // Header
                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(CellStyle).Text("Fecha");
                                headerRow.Cell().Element(CellStyle).Text("Número");
                                headerRow.Cell().Element(CellStyle).Text("Proveedor");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Líneas");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Subtotal");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("IVA");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Total");
                            });

                            // Rows
                            decimal totalSubtotal = 0m;
                            decimal totalIva = 0m;
                            decimal totalTotal = 0m;

                            foreach (var c in compras)
                            {
                                int lineas = c.Detalles?.Count ?? 0;
                                var fecha = c.Fecha.ToLocalTime().ToString("dd/MM/yyyy");
                                table.Cell().Element(CellStyle).Text(fecha);
                                table.Cell().Element(CellStyle).Text(c.NumeroCompra ?? "-");
                                table.Cell().Element(CellStyle).Text(c.Proveedor?.Nombre ?? "N/A");
                                table.Cell().Element(CellStyle).AlignRight().Text(lineas.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{(c.Subtotal):F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{(c.IVA):F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{(c.Total):F2}");

                                totalSubtotal += c.Subtotal;
                                totalIva += c.IVA;
                                totalTotal += c.Total;
                            }

                            // Totales finales
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).Element(CellStyle).AlignRight().Text("Totales:");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalSubtotal:F2}");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalIva:F2}");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalTotal:F2}");
                            });

                            // estilo celda
                            IContainer CellStyle(IContainer c2) => c2.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        });

                        // Agrega aquí si quieres más secciones (detalles por compra) ...
                    });

                    page.Footer().AlignCenter().Text($"Generado: {DateTime.Now.ToString("g", CultureInfo.InvariantCulture)}").FontSize(8);
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Compras_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        // GET: Reportes/Ventas?clienteId=1&desde=2025-01-01&hasta=2025-01-31
        public async Task<IActionResult> IndexVentas(int? clienteId, DateTime? desde, DateTime? hasta)
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

            if (clienteId.HasValue)
                query = query.Where(v => v.Id_Cliente == clienteId.Value);

            if (desde.HasValue)
                query = query.Where(v => v.Fecha.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(v => v.Fecha.Date <= hasta.Value.Date);

            var ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

            ViewBag.Clientes = await _context.Clientes.Where(c => c.Activo).OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.CurrentCliente = clienteId;
            ViewBag.CurrentDesde = desde?.ToString("yyyy-MM-dd");
            ViewBag.CurrentHasta = hasta?.ToString("yyyy-MM-dd");

            return View("Ventas", ventas);
        }

        // GET: Reportes/DownloadVentasPdf?clienteId=1&desde=2025-01-01&hasta=2025-01-31
        public async Task<IActionResult> DownloadVentasPdf(int? clienteId, DateTime? desde, DateTime? hasta)
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

            if (clienteId.HasValue)
                query = query.Where(v => v.Id_Cliente == clienteId.Value);

            if (desde.HasValue)
                query = query.Where(v => v.Fecha.Date >= desde.Value.Date);

            if (hasta.HasValue)
                query = query.Where(v => v.Fecha.Date <= hasta.Value.Date);

            var ventas = await query.OrderByDescending(v => v.Fecha).ToListAsync();

            QuestPDF.Settings.License = LicenseType.Community;

            // Info del negocio - ajusta si quieres leer desde config
            string nombreNegocio = "Librería y Papelería Propama";
            string nitNegocio = "6613799";
            string direccionNegocio = "2da. Calle 5-41, Zona 1, Mazatenango, Suchitepéquez";

            var pdfBytes = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(30);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Header
                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text(nombreNegocio).FontSize(14).Bold();
                        header.Item().AlignCenter().Text($"NIT: {nitNegocio}").FontSize(9);
                        header.Item().AlignCenter().Text(direccionNegocio).FontSize(9);
                        header.Item().PaddingVertical(6).LineHorizontal(1);
                        header.Item().Text("Reporte de Ventas").FontSize(12).Bold().AlignCenter();
                        string periodo = (desde.HasValue || hasta.HasValue)
                            ? $"Periodo: {(desde?.ToString("dd/MM/yyyy") ?? "Inicio")} - {(hasta?.ToString("dd/MM/yyyy") ?? "Fin")}"
                            : $"Periodo: Todos";
                        header.Item().AlignCenter().Text(periodo).FontSize(9);
                    });

                    page.Content().PaddingVertical(6).Column(content =>
                    {
                        // Tabla resumen de ventas
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2); // Fecha
                                columns.RelativeColumn(2); // Número
                                columns.RelativeColumn(3); // Cliente
                                columns.RelativeColumn(1); // Líneas
                                columns.RelativeColumn(2); // Subtotal
                                columns.RelativeColumn(2); // IVA
                                columns.RelativeColumn(2); // Total
                                columns.RelativeColumn(2); // Utilidad
                            });

                            // Header
                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(CellStyle).Text("Fecha");
                                headerRow.Cell().Element(CellStyle).Text("Número");
                                headerRow.Cell().Element(CellStyle).Text("Cliente");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Líneas");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Subtotal");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("IVA");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Total");
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Utilidad");
                            });

                            decimal totalSubtotal = 0m;
                            decimal totalIva = 0m;
                            decimal totalTotal = 0m;
                            decimal totalUtilidad = 0m;

                            foreach (var v in ventas)
                            {
                                int lineas = v.Detalles?.Count ?? 0;
                                var fecha = v.Fecha.ToLocalTime().ToString("dd/MM/yyyy");
                                table.Cell().Element(CellStyle).Text(fecha);
                                table.Cell().Element(CellStyle).Text(v.NumeroVenta ?? "-");
                                string clienteNombre = v.Cliente != null ? $"{v.Cliente.Nombre} {v.Cliente.Apellido}".Trim() : v.NombreConsumidor ?? "Consumidor Final";
                                table.Cell().Element(CellStyle).Text(clienteNombre);
                                table.Cell().Element(CellStyle).AlignRight().Text(lineas.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{v.Subtotal:F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{v.IVA:F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{v.Total:F2}");

                                // Calcular utilidad por venta: para cada detalle (precioVentaPorPresentacion - precioCostoPorPresentacion) * CantidadPresentaciones
                                decimal utilidadVenta = 0m;
                                foreach (var det in v.Detalles)
                                {
                                    decimal precioVentaPres = det.PrecioVentaPorPresentacion;
                                    decimal precioCostoPres = det.Presentacion?.PrecioCosto ?? 0m; // si es null, asumimos 0 (servicio o costo no registrado)
                                    decimal utilidadLinea = (precioVentaPres - precioCostoPres) * det.CantidadPresentaciones;
                                    utilidadVenta += utilidadLinea;
                                }

                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{utilidadVenta:F2}");

                                totalSubtotal += v.Subtotal;
                                totalIva += v.IVA;
                                totalTotal += v.Total;
                                totalUtilidad += utilidadVenta;
                            }

                            // Totales
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(4).Element(CellStyle).AlignRight().Text("Totales:");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalSubtotal:F2}");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalIva:F2}");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalTotal:F2}");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalUtilidad:F2}");
                            });

                            // estilo celda
                            IContainer CellStyle(IContainer c2) => c2.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        });

                        // Opcional: sección con detalles por venta (si quieres)
                    });

                    page.Footer().AlignCenter().Text($"Generado: {DateTime.Now.ToString("g", CultureInfo.InvariantCulture)}").FontSize(8);
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Ventas_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }

        // GET: Reportes/IndexInventario?categoriaId=1&bajoStock=true&minStock=5
        public async Task<IActionResult> IndexInventario(int? categoriaId, bool? bajoStock, int? minStock, string q = null)
        {
            var query = _context.Items
                .Where(i => i.Activo && !i.IsServicio) // excluir inactivos y servicios
                .Include(i => i.Categoria)
                .Include(i => i.Presentaciones.Where(p => p.Activo))
                    .ThenInclude(p => p.UnidadMedida)
                .AsQueryable();

            if (categoriaId.HasValue)
                query = query.Where(i => i.Id_Categoria == categoriaId.Value);

            if (bajoStock.HasValue && bajoStock.Value)
            {
                if (minStock.HasValue)
                    query = query.Where(i => i.Stock <= minStock.Value);
                else
                    query = query.Where(i => i.Stock <= (i.StockMinimo ?? 0));
            }

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(i => i.Nombre.Contains(q) || i.Codigo.Contains(q));
            }

            var items = await query.OrderBy(i => i.Nombre).ToListAsync();

            ViewBag.Categorias = await _context.Categorias.OrderBy(c => c.Nombre).ToListAsync();
            ViewBag.CurrentCategoria = categoriaId;
            ViewBag.CurrentBajoStock = bajoStock;
            ViewBag.CurrentMinStock = minStock;
            ViewBag.CurrentQ = q;

            return View("Inventario", items);
        }

        // GET: Reportes/DownloadInventarioPdf?categoriaId=1&bajoStock=true&minStock=5
        public async Task<IActionResult> DownloadInventarioPdf(int? categoriaId, bool? bajoStock, int? minStock, string q = null)
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
            {
                if (minStock.HasValue)
                    query = query.Where(i => i.Stock <= minStock.Value);
                else
                    query = query.Where(i => i.Stock <= (i.StockMinimo ?? 0));
            }

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
                    page.Margin(25);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(9));

                    // Header
                    page.Header().Column(header =>
                    {
                        header.Item().AlignCenter().Text(nombreNegocio).FontSize(14).Bold();
                        header.Item().AlignCenter().Text($"NIT: {nitNegocio}").FontSize(9);
                        header.Item().AlignCenter().Text(direccionNegocio).FontSize(9);
                        header.Item().PaddingVertical(6).LineHorizontal(1);
                        header.Item().Text("Reporte de Inventario").FontSize(12).Bold().AlignCenter();
                        string filtros = (bajoStock == true) ? "Filtro: Bajo stock" : "Filtro: Todos";
                        header.Item().AlignCenter().Text(filtros).FontSize(9);
                    });

                    page.Content().PaddingVertical(6).Column(content =>
                    {
                        content.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(3); // Producto
                                columns.RelativeColumn(2); // Categoria
                                columns.RelativeColumn(1); // Stock
                                columns.RelativeColumn(1); // Stock Min
                                columns.RelativeColumn(2); // Costo prom/unidad
                                columns.RelativeColumn(2); // Precio venta
                                columns.RelativeColumn(2); // Valor inventario
                                columns.RelativeColumn(2); // Utilidad potencial
                            });

                            // Header fila
                            table.Header(headerRow =>
                            {
                                headerRow.Cell().Element(CellStyle).Text("Producto").Bold();
                                headerRow.Cell().Element(CellStyle).Text("Categoría").Bold();
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Stock").Bold();
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Stock Min").Bold();
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Costo (Q)").Bold();
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Precio (Q)").Bold();
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Valor Inv. (Q)").Bold();
                                headerRow.Cell().Element(CellStyle).AlignRight().Text("Utilidad Pot. (Q)").Bold();
                            });

                            decimal totalValorInv = 0m;
                            decimal totalUtilidadPot = 0m;

                            foreach (var it in items)
                            {
                                
                                decimal costoProm = it.CostoPromedioUnidad;
                                var presRef = it.Presentaciones?.FirstOrDefault();
                                decimal precioVentaRef = presRef?.PrecioVenta ?? 0m;

                                decimal valorInv = Math.Round(it.Stock * costoProm, 2);
                                decimal utilidadPot = Math.Round((precioVentaRef - costoProm) * it.Stock, 2);

                                totalValorInv += valorInv;
                                totalUtilidadPot += utilidadPot;

                                table.Cell().Element(CellStyle).Text(it.Nombre);
                                table.Cell().Element(CellStyle).Text(it.Categoria?.Nombre ?? "-");
                                table.Cell().Element(CellStyle).AlignRight().Text(it.Stock.ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text((it.StockMinimo ?? 0).ToString());
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{costoProm:F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{precioVentaRef:F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{valorInv:F2}");
                                table.Cell().Element(CellStyle).AlignRight().Text($"Q{utilidadPot:F2}");
                            }

                            // Totales
                            table.Footer(footer =>
                            {
                                footer.Cell().ColumnSpan(6).Element(CellStyle).AlignRight().Text("Totales:");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalValorInv:F2}");
                                footer.Cell().Element(CellStyle).AlignRight().Text($"Q{totalUtilidadPot:F2}");
                            });

                            // estilo de celda
                            IContainer CellStyle(IContainer c2) => c2.Padding(4).BorderBottom(1).BorderColor(Colors.Grey.Lighten3);
                        });
                    });

                    page.Footer().AlignCenter().Text($"Generado: {DateTime.Now.ToString("g", CultureInfo.InvariantCulture)}").FontSize(8);
                });
            }).GeneratePdf();

            return File(pdfBytes, "application/pdf", $"Reporte_Inventario_{DateTime.UtcNow:yyyyMMdd}.pdf");
        }


    }
}