using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class VentaDetalle
    {
        [Key]
        public int Id_VentaDetalle { get; set; }
        public int Id_Venta { get; set; }
        public Venta? Venta { get; set; }

        public int Id_Item { get; set; }
        public Item? Item { get; set; }

        public int? Id_ItemPresentacion { get; set; }
        public ItemPresentacion? Presentacion { get; set; }

        public int CantidadPresentaciones { get; set; } 
        public int CantidadUnidades { get; set; } 
        public decimal PrecioVentaPorPresentacion { get; set; } 
        public decimal Descuento { get; set; } 
        public decimal Subtotal { get; set; } 
        public bool EsServicio { get; set; }
    }
}

