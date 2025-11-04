using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class ProveedorController : Controller
    {
        private readonly AppDbContext _context;

        public ProveedorController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Proveedor
        public async Task<IActionResult> Index()
        {
            var proveedores = await _context.Proveedores
                .Where(p => p.Activo)
                .ToListAsync();
            return View(proveedores);
        }


        // GET: Proveedor/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Proveedor/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ProveedorViewModel model)
        {
            if (ModelState.IsValid)
            {
                //if (await _context.Proveedores.Where(p => p.Activo).AnyAsync(p => p.Correo == model.Correo))
                //{
                //    ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                //    return View(model);
                //}

                var proveedor = new Proveedor
                {
                    Nombre = model.Nombre,
                    Telefono = model.Telefono,
                    Correo = model.Correo,
                    Direccion = model.Direccion,
                    Activo = model.Activo
                };

                _context.Proveedores.Add(proveedor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Proveedor creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Proveedor/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
            {
                return NotFound();
            }

            var model = new ProveedorViewModel
            {
                Id_Proveedor = proveedor.Id_Proveedor,
                Nombre = proveedor.Nombre,
                Telefono = proveedor.Telefono,
                Correo = proveedor.Correo,
                Direccion = proveedor.Direccion,
                Activo = proveedor.Activo
            };

            return View(model);
        }

        // POST: Proveedor/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, ProveedorViewModel model)
        {
            if (id != model.Id_Proveedor)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var proveedor = await _context.Proveedores.FindAsync(id);

                    if (proveedor == null)
                    {
                        return NotFound();
                    }

                    //if (await _context.Proveedores.AnyAsync(p => p.Correo == model.Correo && p.Id_Proveedor != id))
                    //{
                    //    ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                    //    return View(model);
                    //}

                    proveedor.Nombre = model.Nombre;
                    proveedor.Telefono = model.Telefono;
                    proveedor.Correo = model.Correo;
                    proveedor.Direccion = model.Direccion;
                    proveedor.Activo = model.Activo;

                    _context.Update(proveedor);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Proveedor actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!ProveedorExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            return View(model);
        }

        // GET: Proveedor/Eliminar/5
        public async Task<IActionResult> Eliminar(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor == null)
            {
                return NotFound();
            }

            return View(proveedor);
        }

        // POST: Proveedor/Eliminar/5
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var proveedor = await _context.Proveedores.FindAsync(id);

            if (proveedor != null)
            {
                proveedor.Activo = false;
                _context.Update(proveedor);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Proveedor eliminado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }

        private bool ProveedorExists(int id)
        {
            return _context.Proveedores.Any(e => e.Id_Proveedor == id);
        }
    }
}