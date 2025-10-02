using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ProveedorViewModel
    {
        public int Id_Proveedor { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [MaxLength(15)]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "El correo es obligatorio")]
        [EmailAddress]
        public string Correo { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [MaxLength(200)]
        public string Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}