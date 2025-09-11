using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Empleado")] // Solo empleados pueden acceder
    public class EmpleadoController : Controller
    {
        public IActionResult Dashboard()
        {
            return View();
        }

        public IActionResult Ventas()
        {
            return View();
        }

        public IActionResult Inventario()
        {
            return View();
        }

        public IActionResult Clientes()
        {
            return View();
        }
    }
}