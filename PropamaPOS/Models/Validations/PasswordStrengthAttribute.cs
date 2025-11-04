using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;

namespace PropamaPOS.Models.Validations
{
    public class PasswordStrengthAttribute : ValidationAttribute
    {
        private const string DefaultErrorMessage =
            "La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un caracter especial.";

        public PasswordStrengthAttribute() : base(DefaultErrorMessage) { }

        public override bool IsValid(object? value)
        {
            if (value == null)
                return false;

            string password = value.ToString() ?? "";

            // Al menos 8 caracteres, 1 mayúscula, 1 minúscula, 1 dígito y 1 caracter especial
            var regex = new Regex(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^\da-zA-Z]).{8,}$");

            return regex.IsMatch(password);
        }
    }
}
