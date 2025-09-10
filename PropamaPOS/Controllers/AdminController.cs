using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;
using System.Security.Cryptography;
using System.Text;

namespace PropamaPOS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly AppDbContext _context;

        public AdminController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Admin/Usuarios
        public async Task<IActionResult> Usuarios()
        {
            var usuarios = await _context.Usuarios
                .Include(u => u.Rol)
                .ToListAsync();
            return View(usuarios);
        }

        // GET: Admin/CrearUsuario
        public async Task<IActionResult> CrearUsuario()
        {
            var roles = await _context.Roles.ToListAsync();
            ViewBag.Roles = roles;
            return View();
        }

        // POST: Admin/CrearUsuario
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearUsuario(CrearUsuarioViewModel model)
        {
            if (ModelState.IsValid)
            {
                // Verificar si el correo ya existe
                if (await _context.Usuarios.AnyAsync(u => u.Correo == model.Correo))
                {
                    ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                    ViewBag.Roles = await _context.Roles.ToListAsync();
                    return View(model);
                }

                // Generar hash y salt para la contraseña
                using (var hmac = new HMACSHA256())
                {
                    var usuario = new Usuario
                    {
                        Correo = model.Correo,
                        ContraSalt = Convert.ToBase64String(hmac.Key),
                        ContraHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(model.Password))),
                        Id_Rol = model.Id_Rol
                    };

                    _context.Usuarios.Add(usuario);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Usuario creado exitosamente.";
                    return RedirectToAction(nameof(Usuarios));
                }
            }

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(model);
        }

        // GET: Admin/EditarUsuario/5
        public async Task<IActionResult> EditarUsuario(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario == null)
            {
                return NotFound();
            }

            var roles = await _context.Roles.ToListAsync();
            ViewBag.Roles = roles;

            var model = new EditarUsuarioViewModel
            {
                Id_Usuario = usuario.Id_Usuario,
                Correo = usuario.Correo,
                Id_Rol = usuario.Id_Rol
            };

            return View(model);
        }

        // POST: Admin/EditarUsuario/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarUsuario(int id, EditarUsuarioViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (id != model.Id_Usuario)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var usuario = await _context.Usuarios.FindAsync(id);
                    if (usuario == null)
                    {
                        return NotFound();
                    }

                    // Verificar si el correo ya existe (excluyendo el usuario actual)
                    if (await _context.Usuarios.AnyAsync(u => u.Correo == model.Correo && u.Id_Usuario != id))
                    {
                        ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                        ViewBag.Roles = await _context.Roles.ToListAsync();
                        return View(model);
                    }

                    usuario.Correo = model.Correo;
                    usuario.Id_Rol = model.Id_Rol;

                    // Si se proporcionó una nueva contraseña, actualizarla
                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        using (var hmac = new HMACSHA256())
                        {
                            usuario.ContraSalt = Convert.ToBase64String(hmac.Key);
                            usuario.ContraHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(model.Password)));
                        }
                    }

                    _context.Update(usuario);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Usuario actualizado exitosamente.";
                    return RedirectToAction(nameof(Usuarios));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UsuarioExists(id))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
            }

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(model);
        }

        // GET: Admin/EliminarUsuario/5
        public async Task<IActionResult> EliminarUsuario(int id)
        {
            var usuario = await _context.Usuarios
                .Include(u => u.Rol)
                .FirstOrDefaultAsync(u => u.Id_Usuario == id);

            if (usuario == null)
            {
                return NotFound();
            }

            return View(usuario);
        }

        // POST: Admin/EliminarUsuario/5
        [HttpPost]
        [ActionName("EliminarUsuario")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarUsuarioConfirmado(int id)
        {
            var usuario = await _context.Usuarios.FindAsync(id);
            if (usuario != null)
            {
                _context.Usuarios.Remove(usuario);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Usuario eliminado exitosamente.";
            }
            return RedirectToAction(nameof(Usuarios));
        }

        private bool UsuarioExists(int id)
        {
            return _context.Usuarios.Any(e => e.Id_Usuario == id);
        }
    }
}
