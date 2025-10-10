// PropamaPOS/Models/AjusteInventarioDetalle.cs
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class AjusteInventarioDetalle
    {
        [Key]
        public int Id_AjusteDetalle { get; set; }

        public int Id_Ajuste { get; set; }
        [ForeignKey("Id_Ajuste")]
        public AjusteInventario Ajuste { get; set; }

        public int Id_Item { get; set; }
        [ForeignKey("Id_Item")]
        public Item Item { get; set; }

        // Presentación (opcional en la entidad, pero en la UI la pedimos)
        public int? Id_ItemPresentacion { get; set; }
        [ForeignKey("Id_ItemPresentacion")]
        public ItemPresentacion? Presentacion { get; set; }

        [Range(1, int.MaxValue)]
        public int CantidadPresentaciones { get; set; }

        // Cantidad en unidades individuales (signed): positiva para entradas, negativa para salidas
        public int CantidadUnidades { get; set; }

        // Auditoría de stock
        public int StockAntes { get; set; }
        public int StockDespues { get; set; }

        public string? Nota { get; set; }
    }
}
