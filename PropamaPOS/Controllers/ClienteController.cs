// PropamaPOS/Controllers/ClienteController.cs
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin,Empleado")]
    public class ClienteController : Controller
    {
        private readonly AppDbContext _context;
        private const int PageSize = 30;

        public ClienteController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Cliente
        public async Task<IActionResult> Index(string q, int page = 1)
        {
            var query = _context.Clientes
                        .Where(c => c.Activo)
                        .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(c =>
                    c.NIT.Contains(q) ||
                    c.Nombre.Contains(q) ||
                    (c.Apellido != null && c.Apellido.Contains(q))
                );
            }

            var total = await query.CountAsync();
            var totalPages = (int)Math.Ceiling(total / (double)PageSize);

            var clientes = await query
                .OrderBy(c => c.Id_Cliente)
                .Skip((page - 1) * PageSize)
                .Take(PageSize)
                .ToListAsync();

            ViewBag.CurrentQuery = q;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = PageSize;
            ViewBag.TotalItems = total;

            return View(clientes);
        }

        // GET: Cliente/Crear
        public IActionResult Crear()
        {
            return View();
        }

        // POST: Cliente/Crear
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Crear(ClienteViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Validar NIT único
                if (await _context.Clientes.AnyAsync(c => c.NIT == model.NIT))
                {
                    ModelState.AddModelError("NIT", "Este NIT ya está registrado.");
                    return View(model);
                }

                var cliente = new Cliente
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    NIT = model.NIT,
                    Direccion = model.Direccion,
                    Activo = true
                };

                _context.Clientes.Add(cliente);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cliente creado exitosamente.";
                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }

        // GET: Cliente/Editar/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente == null)
            {
                return NotFound();
            }

            var model = new ClienteViewModel
            {
                Id_Cliente = cliente.Id_Cliente,
                Nombre = cliente.Nombre,
                Apellido = cliente.Apellido,
                NIT = cliente.NIT,
                Direccion = cliente.Direccion,
                Activo = cliente.Activo
            };

            return View(model);
        }

        // POST: Cliente/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Editar(int id, ClienteViewModel model)
        {
            if (id != model.Id_Cliente) return NotFound();

            if (ModelState.IsValid)
            {
                try
                {
                    var cliente = await _context.Clientes.FindAsync(id);
                    if (cliente == null) return NotFound();

                    // Validar NIT único
                    if (await _context.Clientes.AnyAsync(c => c.NIT == model.NIT && c.Id_Cliente != id))
                    {
                        ModelState.AddModelError("NIT", "Este NIT ya está registrado por otro cliente.");
                        return View(model);
                    }

                    cliente.Nombre = model.Nombre;
                    cliente.Apellido = model.Apellido;
                    cliente.NIT = model.NIT;
                    cliente.Direccion = model.Direccion;
                    cliente.Activo = true;

                    _context.Update(cliente);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Cliente actualizado exitosamente.";
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!_context.Clientes.Any(e => e.Id_Cliente == id))
                        return NotFound();
                    else
                        throw;
                }
            }

            return View(model);
        }

        // GET: Cliente/Eliminar/5
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> Eliminar(int id)
        {
            var cliente = await _context.Clientes.FirstOrDefaultAsync(c => c.Id_Cliente == id);
            if (cliente == null) return NotFound();

            return View(cliente);
        }

        // POST: Cliente/Eliminar/5
        [HttpPost]
        [ActionName("Eliminar")]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> EliminarConfirmado(int id)
        {
            var cliente = await _context.Clientes.FindAsync(id);

            if (cliente != null)
            {
                cliente.Activo = false;
                _context.Update(cliente);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Cliente desactivado exitosamente.";
            }

            return RedirectToAction(nameof(Index));
        }


        private bool ClienteExists(int id)
        {
            return _context.Clientes.Any(e => e.Id_Cliente == id);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarPorNit(string nit)
        {
            if (string.IsNullOrWhiteSpace(nit))
                return Json(new { found = false });

            var cliente = await _context.Clientes
                .FirstOrDefaultAsync(c => c.NIT == nit && c.Activo);

            if (cliente == null)
                return Json(new { found = false });

            return Json(new
            {
                found = true,
                id = cliente.Id_Cliente,
                nombre = cliente.Nombre,
                apellido = cliente.Apellido,
                direccion = cliente.Direccion
            });
        }

    }
}
