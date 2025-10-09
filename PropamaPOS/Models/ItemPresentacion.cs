using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class ItemPresentacion
    {
        [Key]
        public int Id_ItemPresentacion { get; set; }

        public int Id_Item { get; set; }
        [ForeignKey("Id_Item")]
        public Item Item { get; set; }

        public int Id_UnidadMedida { get; set; }
        [ForeignKey("Id_UnidadMedida")]
        public UnidadMedida UnidadMedida { get; set; }

        // Cantidad de unidades individuales contenidas en esta presentación
        public int Cantidad { get; set; } = 1;

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioVenta { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal? PrecioCosto { get; set; }

        public bool Activo { get; set; } = true;

        [NotMapped]
        public decimal PrecioVentaUnitario => Cantidad == 0 ? 0 : Math.Round(PrecioVenta / Cantidad, 2);
    }
}
