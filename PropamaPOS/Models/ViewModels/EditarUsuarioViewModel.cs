using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class EditarUsuarioViewModel
    {
        public int Id_Usuario { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [MaxLength(50, ErrorMessage = "El nombre de usuario no puede exceder 50 caracteres")]
        public string NombreUsuario { get; set; }

        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "La contraseña debe tener al menos 6 caracteres")]
        public string Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        [Display(Name = "Rol")]
        public int Id_Rol { get; set; }
    }
}
