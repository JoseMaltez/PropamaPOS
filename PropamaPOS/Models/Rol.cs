using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models
{
    public class Rol
    {
        [Key]
        public int Id_Rol { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [MaxLength(200)]
        public string Descripcion { get; set; }
    }
}
