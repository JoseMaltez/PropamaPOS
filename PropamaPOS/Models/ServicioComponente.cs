using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class ServicioComponente
    {
        [Key]
        public int Id_ServicioComponente { get; set; }

        // Servicio (Item donde IsServicio == true)
        [Required]
        public int Id_Servicio { get; set; }

        // Item consumido (producto físico)
        [Required]
        public int Id_Item { get; set; }

        // Cantidad consumida por 1 unidad del servicio
        // (usar decimal para permitir fracciones si lo necesitas)
        [Required]
        public decimal CantidadPorServicio { get; set; }

        // Navegación
        [ForeignKey("Id_Servicio")]
        public Item Servicio { get; set; } = null!;

        [ForeignKey("Id_Item")]
        public Item ItemConsumido { get; set; } = null!;
    }
}
