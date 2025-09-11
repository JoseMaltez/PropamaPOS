using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class Empleado
    {
        [Key]
        public int Id_Empleado { get; set; }

        [Required]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [Required]
        [MaxLength(100)]
        public string Apellido { get; set; }

        [Required]
        [EmailAddress]
        public string Correo { get; set; }

        [MaxLength(15)]
        public string Telefono { get; set; }

        public DateTime FechaContratacion { get; set; }

        public bool Activo { get; set; } = true;

        public int Id_Usuario { get; set; }

        [ForeignKey("Id_Usuario")]
        public Usuario Usuario { get; set; }
    }
}
