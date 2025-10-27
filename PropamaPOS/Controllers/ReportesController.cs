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
    [Authorize(Roles = "Admin")] // cambiar si quieres que empleados también accedan
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
    }
}