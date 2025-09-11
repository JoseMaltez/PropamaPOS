using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }
        [Required]
        [MaxLength(50)]
        public string NombreUsuario { get; set; }
        [Required]
        public string ContraHash { get; set; }
        [Required]
        public string ContraSalt { get; set; }
        [Required]
        public int Id_Rol { get; set; }

        [ForeignKey("Id_Rol")]
        public Rol Rol { get; set; }

        public Empleado Empleado { get; set; }
    }
}
