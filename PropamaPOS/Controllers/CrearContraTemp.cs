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
            
            using (var hmac = new HMACSHA256())
            {
                string salt = Convert.ToBase64String(hmac.Key);
                var hash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
                string passwordHash = Convert.ToBase64String(hash);

                string resultado = $"Password: {password}\nSalt: {salt}\nHash: {passwordHash}";
                return Content(resultado);
            }
        }
    }
}
