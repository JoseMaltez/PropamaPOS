using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Cryptography;
using System.Text;

namespace PropamaPOS.Controllers
{
    public class CrearContraTemp : Controller
    {
        [AllowAnonymous]
        public IActionResult GenerarHash(string password)
        {
            if (string.IsNullOrEmpty(password))
                return Content("No hay contraseña en el url");

            var hash = BCrypt.Net.BCrypt.HashPassword(password);
            string resultado = $"Password: {password}\nBCryptHash: {hash}";
            return Content(resultado);
        }

        [AllowAnonymous]
        public IActionResult ProbarHash(string password, string hash)
        {
            bool valido = BCrypt.Net.BCrypt.Verify(password, hash);
            return Content(valido ? "✔️ Correcto" : "❌ Incorrecto");
        }
    }
}
