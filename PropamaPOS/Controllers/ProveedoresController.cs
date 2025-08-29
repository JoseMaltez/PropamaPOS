using Microsoft.AspNetCore.Mvc;
using PropamaPOS.Services;
using System.Threading.Tasks;

namespace PropamaPOS.Controllers
{
    public class ProveedoresController : Controller
    {
        private readonly ProveedorServiceClient _service;

        public ProveedoresController(ProveedorServiceClient service)
        {
            _service = service;
        }

        public async Task<IActionResult> Index()
        {
            var proveedores = await _service.GetProveedoresAsync();
            return View(proveedores);
        }

        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Create(ProveedorDto proveedor)
        {
            if (ModelState.IsValid)
            {
                await _service.CreateProveedorAsync(proveedor);
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var proveedor = await _service.GetProveedorAsync(id);
            return View(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Edit(int id, ProveedorDto proveedor)
        {
            if (ModelState.IsValid)
            {
                await _service.UpdateProveedorAsync(id, proveedor);
                return RedirectToAction(nameof(Index));
            }
            return View(proveedor);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var proveedor = await _service.GetProveedorAsync(id);
            return View(proveedor);
        }

        [HttpPost]
        public async Task<IActionResult> Delete(int id, ProveedorDto proveedor)
        {
            await _service.DeleteProveedorAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
