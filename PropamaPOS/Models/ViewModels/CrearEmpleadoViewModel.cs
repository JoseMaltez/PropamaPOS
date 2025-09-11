using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class CrearEmpleadoViewModel
    {
        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [MaxLength(50)]
        public string NombreUsuario { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [MinLength(6)]
        public string Password { get; set; }

        [Required(ErrorMessage = "Confirmar contraseña es obligatorio")]
        [DataType(DataType.Password)]
        [Compare("Password")]
        public string ConfirmPassword { get; set; }

        [Required]
        public int Id_Rol { get; set; }

        // Datos de empleado
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
        public DateTime FechaContratacion { get; set; } = DateTime.Today;
    }
}
