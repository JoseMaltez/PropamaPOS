using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class CompraDetalle
    {
        [Key]
        public int Id_CompraDetalle { get; set; }

        public int Id_Compra { get; set; }
        [ForeignKey("Id_Compra")]
        public Compra Compra { get; set; }

        public int Id_Item { get; set; }
        [ForeignKey("Id_Item")]
        public Item Item { get; set; }

        public int Id_ItemPresentacion { get; set; }
        [ForeignKey("Id_ItemPresentacion")]
        public ItemPresentacion Presentacion { get; set; }

        [Range(1, int.MaxValue)]
        public int CantidadPresentaciones { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal PrecioCostoPorPresentacion { get; set; }

        [Column(TypeName = "decimal(18,4)")]
        public decimal PrecioCostoPorUnidad { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Subtotal { get; set; }
    }
}

