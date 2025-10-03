using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models
{
    public class Cliente
    {
        [Key]
        public int Id_Cliente { get; set; }

        [Required(ErrorMessage = "El nombre es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required(ErrorMessage = "El apellido es obligatorio")]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [Phone(ErrorMessage = "Formato de teléfono inválido")]
        [MaxLength(15)]
        public string Telefono { get; set; }

        [Required(ErrorMessage = "La dirección es obligatoria")]
        [MaxLength(200)]
        public string Direccion { get; set; }

        public bool Activo { get; set; } = true;
    }
}
