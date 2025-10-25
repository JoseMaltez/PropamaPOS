// PropamaPOS/Models/ViewModels/ClienteViewModel.cs
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ClienteViewModel
    {
        public int Id_Cliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(100)]
        public string? Apellido { get; set; }

        [Required(ErrorMessage = "El NIT es obligatorio")]
        [MaxLength(20)]
        public string NIT { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [MaxLength(200)]
        public string Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
