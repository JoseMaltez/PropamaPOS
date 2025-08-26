using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress(ErrorMessage = "El correo no es válido")]
        public string Email { get; set; }
    }
}
