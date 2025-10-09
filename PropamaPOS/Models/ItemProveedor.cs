using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class ItemProveedor
    {
        [Key]
        public int Id_ItemProveedor { get; set; }

        public int Id_Item { get; set; }
        [ForeignKey("Id_Item")]
        public Item Item { get; set; }

        public int Id_Proveedor { get; set; }
        [ForeignKey("Id_Proveedor")]
        public Proveedor Proveedor { get; set; }

        [MaxLength(100)]
        public string? CodigoProveedor { get; set; } // SKU o código interno del proveedor

        public bool Activo { get; set; } = true;
    }
}
