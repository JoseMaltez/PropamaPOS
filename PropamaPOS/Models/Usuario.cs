using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models
{
    public class Usuario
    {
        [Key]
        public int Id_Usuario { get; set; }
        [Required]
        public string Correo { get; set; }
        [Required]
        public string ContraHash { get; set; }
        [Required]
        public string ContraSalt { get; set; }
        [Required]
        public string Rol { get; set; }
    }
}
