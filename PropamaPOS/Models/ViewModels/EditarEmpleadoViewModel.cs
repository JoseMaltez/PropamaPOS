using PropamaPOS.Models.Validations;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class EditarEmpleadoViewModel
    {
        public int Id_Empleado { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Correo { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "La fecha de contratación es obligatoria")]
        [DataType(DataType.Date)]
        public DateTime FechaContratacion { get; set; }

        public bool Activo { get; set; } = true;

        public int Id_Usuario { get; set; }

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [MaxLength(50)]
        public string NombreUsuario { get; set; }

        [DataType(DataType.Password)]
        [PasswordStrength]
        public string? Password { get; set; }

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Las contraseñas no coinciden")]
        public string? ConfirmPassword { get; set; }

        [Required(ErrorMessage = "El rol es obligatorio")]
        public int Id_Rol { get; set; }
    }
}
