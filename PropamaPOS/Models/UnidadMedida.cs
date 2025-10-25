using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models
{
    public class UnidadMedida
    {
        [Key]
        public int Id_UnidadMedida { get; set; }

        [Required]
        [MaxLength(50)]
        public string Nombre { get; set; }

        [MaxLength(10)]
        public string? Abreviatura { get; set; }
    }
}
