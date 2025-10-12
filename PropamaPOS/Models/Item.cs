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

        public bool IsServicio { get; set; } = false;

        // Stock total en unidades individuales (para productos). En servicios se mantiene en 0.
        public int Stock { get; set; } = 0;

        // Costo promedio por unidad (cálculo automático para productos)
        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoPromedioUnidad { get; set; } = 0m;

        // Relaciones
        public ICollection<ItemPresentacion> Presentaciones { get; set; } = new List<ItemPresentacion>();

        public int? Id_Categoria { get; set; }
        public Categoria? Categoria { get; set; }
    }
}
