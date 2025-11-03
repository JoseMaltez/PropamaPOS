using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Empleado")]
    public class EmpleadoController : Controller
    {
        private readonly AppDbContext _context;

        public EmpleadoController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Empleado/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var hoy = DateTime.Now.Date;

            var ventasHoy = await _context.Ventas
                .CountAsync(v => v.Fecha.Date == hoy);

            var montoHoy = await _context.Ventas
                .Where(v => v.Fecha.Date == hoy)
                .SumAsync(v => (decimal?)v.Total) ?? 0m;

            var bajoStock = await _context.Items
                .Where(i => i.Activo && !i.IsServicio)
                .CountAsync(i => i.Stock <= (i.StockMinimo ?? 0));

            var ultimasVentas = await _context.Ventas
                .Include(v => v.Cliente)
                .OrderByDescending(v => v.Fecha)
                .Take(5)
                .ToListAsync();

            ViewBag.VentasHoy = ventasHoy;
            ViewBag.MontoHoy = montoHoy;
            ViewBag.BajoStock = bajoStock;
            ViewBag.UltimasVentas = ultimasVentas;

            return View();
        }
    }
}