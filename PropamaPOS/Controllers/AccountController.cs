using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using PropamaPOS.Data;
using PropamaPOS.Models;
using PropamaPOS.Models.ViewModels;
using PropamaPOS.Services;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace PropamaPOS.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;
        private readonly EmailServiceClient _emailClient;

        public AccountController(AppDbContext context, EmailServiceClient emailClient)
        {
            _context = context;
            _emailClient = emailClient;
        }

        [HttpGet] //No es necesario colocar [HttpGet] ya que es el valor por defecto
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> Login(string nombreUsuario, string password)
        {
            var usuario = _context.Usuarios.Include(u => u.Rol)
                .Include(u => u.Empleado)
                .FirstOrDefault(u => u.NombreUsuario == nombreUsuario);

            // Buscar usuario
            if (usuario == null)
            {
                ViewBag.Error = "Usuario y/o contraseña incorrectos";
                return View();
            }

            // Verificar contraseña usando BCrypt
            if (string.IsNullOrEmpty(usuario.PasswordHash) ||
                !BCrypt.Net.BCrypt.Verify(password, usuario.PasswordHash))
            {
                ViewBag.Error = "Usuario y/o contraseña incorrectos";
                return View();
            }


            //Crear Claims para la cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Empleado.Nombre + " " + usuario.Empleado.Apellido),
                new Claim(ClaimTypes.Role, usuario.Rol.Nombre ?? "Empleado"),
                new Claim(ClaimTypes.NameIdentifier, usuario.Id_Usuario.ToString())
            };

            var claimsIdentity = new ClaimsIdentity(
                claims, CookieAuthenticationDefaults.AuthenticationScheme);

            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true, // Mantener sesión iniciada
                ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(30) // Expiración
            };

            //Guardar cookie de autenticación
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                new ClaimsPrincipal(claimsIdentity),
                authProperties);

            // return RedirectToAction("Index", "Home"); // (metodo, controlador) url: /Home/Index

            if (usuario.Rol?.Nombre?.Equals("Admin", StringComparison.OrdinalIgnoreCase) == true)
            {
                return RedirectToAction("Dashboard", "Admin");
            }
            else if (usuario.Rol?.Nombre?.Equals("Empleado", StringComparison.OrdinalIgnoreCase) == true)
            {
                return RedirectToAction("Dashboard", "Empleado");
            }
            else
            {
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        // Procesar envío del correo
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var empleado = await _context.Empleados
                .Include(e => e.Usuario)
                .FirstOrDefaultAsync(e => e.Correo == model.Email);

            if (empleado == null || empleado.Usuario == null)
            {
                TempData["Message"] = "Si el correo existe, recibirás un enlace de recuperación.";
                return RedirectToAction("ForgotPassword");
            }

            var usuario = empleado.Usuario;

            // Generar token aleatorio
            var token = Convert.ToBase64String(RandomNumberGenerator.GetBytes(64));

            var resetToken = new PasswordResetToken
            {
                Id_Usuario = usuario.Id_Usuario,
                Token = token,
                Expiracion = DateTime.UtcNow.AddHours(1)
            };

            //Insertar token la base de datos
            _context.PasswordResetTokens.Add(resetToken);
            await _context.SaveChangesAsync();

            //Crear url con token (metodo, controlador, parametros, esquema)
            var resetLink = Url.Action("ResetPassword", "Account", new { token = token }, Request.Scheme);
            var subject = "Restablecimiento de contraseña";
            var body = $@"
            <p>Hola,</p>
            <p>Solicitaste restablecer tu contraseña. Haz clic en el siguiente enlace o cópialo en tu navegador:</p>
            <p><a href=""{resetLink}"">{resetLink}</a></p>
            <p><strong>Importante:</strong> este enlace expira en 1 hora.</p>
            <p>Si no fuiste tú, puedes ignorar este mensaje.</p>
            <p>Este es un correo automático, por favor no responder.</p>";

            var (success, message) = await _emailClient.SendAsync(empleado.Correo, subject, body);

            TempData["Message"] = success
                ? "Si el correo existe, recibirás un enlace de recuperación."
                : $"{message}";

            return RedirectToAction("ForgotPassword");


        }

        // Mostrar formulario para ingresar nueva contraseña
        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string token)
        {
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login");

            return View(new ResetPasswordViewModel { Token = token });
        }

        // Procesar el cambio de contraseña
        [HttpPost]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var resetToken = await _context.PasswordResetTokens
                .Include(t => t.Usuario)
                .FirstOrDefaultAsync(t => t.Token == model.Token && t.Expiracion > DateTime.UtcNow);

            if (resetToken == null)
            {
                ModelState.AddModelError("", "El token no es válido o ha expirado.");
                ModelState.AddModelError("", "Por favor solicite un nuevo correo para reiniciar su contraseña");
                return View(model);
            }

            // Generar nuevo hash
            resetToken.Usuario.PasswordHash = BCrypt.Net.BCrypt.HashPassword(model.Password);


            // Eliminar token usado
            _context.PasswordResetTokens.Remove(resetToken);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Contraseña restablecida correctamente.";
            return RedirectToAction("Login");
        }
    }
}
