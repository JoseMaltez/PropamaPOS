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

        // GET: Admin/Dashboard
        public async Task<IActionResult> Dashboard()
        {
            var totalEmpleados = await _context.Empleados.CountAsync();
            var totalProveedores = await _context.Proveedores.Where(p => p.Activo).CountAsync();
            var totalClientes = await _context.Clientes.CountAsync();
            var totalItems = await _context.Items.CountAsync();
            var comprasPendientes = await _context.Compras.CountAsync(c => c.Estado == CompraEstado.Pendiente);
            var ventasHoy = await _context.Ventas.CountAsync(v => v.Fecha.Date == DateTime.Now.Date);

            var bajoStock = await _context.Items
                .Where(i => i.Activo && !i.IsServicio)
                .CountAsync(i => i.Stock <= (i.StockMinimo ?? 0));

            var ultimasVentas = await _context.Ventas
                .Include(v => v.Cliente)
                .OrderByDescending(v => v.Fecha)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalEmpleados = totalEmpleados;
            ViewBag.TotalProveedores = totalProveedores;
            ViewBag.TotalClientes = totalClientes;
            ViewBag.TotalItems = totalItems;
            ViewBag.ComprasPendientes = comprasPendientes;
            ViewBag.VentasHoy = ventasHoy;
            ViewBag.BajoStock = bajoStock;
            ViewBag.UltimasVentas = ultimasVentas;

            return View();
        }


        // GET: Admin/Empleados
        public async Task<IActionResult> Empleados()
        {
            var empleados = await _context.Empleados
                .Include(e => e.Usuario)
                    .ThenInclude(u => u.Rol)
                .Where(e => e.Activo)
                .ToListAsync();

            return View(empleados);
        }


        // GET: Admin/CrearEmpleado
        public async Task<IActionResult> CrearEmpleado()
        {
            var roles = await _context.Roles.ToListAsync();
            ViewBag.Roles = roles;
            return View();
        }

        // POST: Admin/CrearEmpleado
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CrearEmpleado(CrearEmpleadoViewModel model)
        {
            if (ModelState.IsValid)
            {
                if (await _context.Empleados.AnyAsync(e => e.Correo == model.Correo))
                {
                    ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                    ViewBag.Roles = await _context.Roles.ToListAsync();
                    return View(model);
                }

                if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario))
                {
                    ModelState.AddModelError("NombreUsuario", "Este nombre de usuario ya está registrado.");
                    ViewBag.Roles = await _context.Roles.ToListAsync();
                    return View(model);
                }

                var usuario = new Usuario
                {
                    NombreUsuario = model.NombreUsuario,
                    PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password),
                    Id_Rol = model.Id_Rol
                };


                _context.Usuarios.Add(usuario);
                await _context.SaveChangesAsync();

                var empleado = new Empleado
                {
                    Nombre = model.Nombre,
                    Apellido = model.Apellido,
                    Correo = model.Correo,
                    Telefono = model.Telefono,
                    FechaContratacion = model.FechaContratacion,
                    Activo = true,
                    Id_Usuario = usuario.Id_Usuario
                };

                _context.Empleados.Add(empleado);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Empleado creado exitosamente.";
                return RedirectToAction(nameof(Empleados));
            }

            ViewBag.Roles = await _context.Roles.ToListAsync();
            return View(model);
        }

        // GET: Admin/EditarEmpleado/5
        public async Task<IActionResult> EditarEmpleado(int id)
        {
            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.Id_Empleado == id);

            if (empleado == null)
            {
                return NotFound();
            }

            var roles = await _context.Roles.ToListAsync();
            ViewBag.Roles = roles;

            var model = new EditarEmpleadoViewModel
            {
                Id_Empleado = empleado.Id_Empleado,
                Nombre = empleado.Nombre,
                Apellido = empleado.Apellido,
                Correo = empleado.Correo,
                Telefono = empleado.Telefono,
                FechaContratacion = empleado.FechaContratacion,
                Activo = empleado.Activo,
                Id_Usuario = empleado.Id_Usuario,
                NombreUsuario = empleado.Usuario.NombreUsuario,
                Id_Rol = empleado.Usuario.Id_Rol
            };

            return View(model);
        }

        // POST: Admin/EditarEmpleado/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditarEmpleado(int id, EditarEmpleadoViewModel model)
        {
            if (string.IsNullOrEmpty(model.Password))
            {
                ModelState.Remove("Password");
                ModelState.Remove("ConfirmPassword");
            }

            if (id != model.Id_Empleado)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    var empleado = await _context.Empleados
                        .Include(e => e.Usuario)
                        .FirstOrDefaultAsync(e => e.Id_Empleado == id);

                    if (empleado == null)
                    {
                        return NotFound();
                    }

                    if (await _context.Empleados.AnyAsync(e => e.Correo == model.Correo && e.Id_Empleado != id))
                    {
                        ModelState.AddModelError("Correo", "Este correo ya está registrado.");
                        ViewBag.Roles = await _context.Roles.ToListAsync();
                        return View(model);
                    }

                    if (await _context.Usuarios.AnyAsync(u => u.NombreUsuario == model.NombreUsuario && u.Id_Usuario != empleado.Id_Usuario))
                    {
                        ModelState.AddModelError("NombreUsuario", "Este nombre de usuario ya está registrado.");
                        ViewBag.Roles = await _context.Roles.ToListAsync();
                        return View(model);
                    }

                    // Actualizar empleado
                    empleado.Nombre = model.Nombre;
                    empleado.Apellido = model.Apellido;
                    empleado.Correo = model.Correo;
                    empleado.Telefono = model.Telefono;
                    empleado.FechaContratacion = model.FechaContratacion;
                    empleado.Activo = model.Activo;

                    // Actualizar usuario
                    empleado.Usuario.NombreUsuario = model.NombreUsuario;
                    empleado.Usuario.Id_Rol = model.Id_Rol;

                    if (!string.IsNullOrEmpty(model.Password))
                    {
                        empleado.Usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);
                    }

                    _context.Update(empleado);
                    await _context.SaveChangesAsync();

                    TempData["SuccessMessage"] = "Empleado actualizado exitosamente.";
                    return RedirectToAction(nameof(Empleados));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!EmpleadoExists(id))
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

        // GET: Admin/EliminarEmpleado/5
        public async Task<IActionResult> EliminarEmpleado(int id)
        {
            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .ThenInclude(u => u.Rol)
                .FirstOrDefaultAsync(e => e.Id_Empleado == id);

            if (empleado == null)
            {
                return NotFound();
            }

            return View(empleado);
        }

        // POST: Admin/EliminarEmpleado/5
        [HttpPost]
        [ActionName("EliminarEmpleado")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EliminarEmpleadoConfirmado(int id)
        {
            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.Id_Empleado == id);

            if (empleado != null)
            {
                empleado.Activo = false;

                _context.Empleados.Update(empleado);
                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] = "Empleado eliminado exitosamente.";
            }
            else
            {
                TempData["ErrorMessage"] = "No se encontró el empleado especificado.";
            }

            return RedirectToAction(nameof(Empleados));
        }


        private bool EmpleadoExists(int id)
        {
            return _context.Empleados.Any(e => e.Id_Empleado == id);
        }
    }
}
