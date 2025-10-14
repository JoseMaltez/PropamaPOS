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

        public int CantidadPresentaciones { get; set; } // cuántas presentaciones vendidas (ej. 2 cajas)
        public int CantidadUnidades { get; set; } // cantidad en unidades (presentacion.Cantidad * CantidadPresentaciones)
        public decimal PrecioVentaPorPresentacion { get; set; } // precio que estaba en presentacion
        public decimal Descuento { get; set; } // descuento individual
        public decimal Subtotal { get; set; } // despues de descuento
        public bool EsServicio { get; set; }
    }
}

