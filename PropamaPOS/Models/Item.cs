// PropamaPOS/Models/Item.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class Item
    {
        [Key]
        public int Id_Item { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [MaxLength(50)]
        public string? Codigo { get; set; }

        public bool Activo { get; set; } = true;

        // Stock total en unidades individuales
        public int Stock { get; set; } = 0;

        // Costo promedio por unidad (cálculo automático)
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoPromedioUnidad { get; set; } = 0m;

        // Relaciones
        public ICollection<ItemPresentacion> Presentaciones { get; set; } = new List<ItemPresentacion>();
    }
}
