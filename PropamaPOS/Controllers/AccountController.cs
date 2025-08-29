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
        public async Task<IActionResult> Login(string correo, string password)
        {
            var usuario = _context.Usuarios.FirstOrDefault(u => u.Correo == correo);

            if (usuario == null)
            {
                ViewBag.Error = "Usuario y/o contraseña incorrectos";
                return View();
            }

            // Verificar contraseña usando Hash + Salt
            using (var hmac = new HMACSHA256(Convert.FromBase64String(usuario.ContraSalt)))
            {
                var computedHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                var storedHash = Convert.FromBase64String(usuario.ContraHash);

                if (!computedHash.SequenceEqual(storedHash))
                {
                    ViewBag.Error = "Usuario y/o contraseña incorrectos";
                    return View();
                }

            }

            //Crear Claims para la cookie
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.Name, usuario.Correo),
                new Claim(ClaimTypes.Role, usuario.Rol ?? "Empleado")
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

            return RedirectToAction("Index", "Home"); // (metodo, controlador) url: /Home/Index
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

            var usuario = await _context.Usuarios.FirstOrDefaultAsync(u => u.Correo == model.Email);

            if (usuario == null)
            {
                TempData["Message"] = "Si el correo existe, recibirás un enlace de recuperación.";
                return RedirectToAction("ForgotPassword");
            }

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
            //Console.WriteLine($"Link de restablecimiento: {resetLink}");
            var subject = "Restablecimiento de contraseña";
            var body = $@"
            <p>Hola,</p>
            <p>Solicitaste restablecer tu contraseña. Haz clic en el siguiente enlace o cópialo en tu navegador:</p>
            <p><a href=""{resetLink}"">{resetLink}</a></p>
            <p><strong>Importante:</strong> este enlace expira en 1 hora.</p>
            <p>Si no fuiste tú, puedes ignorar este mensaje.</p>
            <p>Este es un correo automático, por favor no responder.</p>";

            var (success, message) = await _emailClient.SendAsync(usuario.Correo, subject, body);

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

            // Generar nuevo salt y hash
            using (var hmac = new HMACSHA256())
            {
                resetToken.Usuario.ContraSalt = Convert.ToBase64String(hmac.Key);
                resetToken.Usuario.ContraHash = Convert.ToBase64String(hmac.ComputeHash(Encoding.UTF8.GetBytes(model.NewPassword)));
            }

            // Eliminar token usado
            _context.PasswordResetTokens.Remove(resetToken);
            await _context.SaveChangesAsync();

            TempData["Message"] = "Contraseña restablecida correctamente.";
            return RedirectToAction("Login");
        }
    }
}
