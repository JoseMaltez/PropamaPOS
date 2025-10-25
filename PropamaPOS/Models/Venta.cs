using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public enum MetodoPagoVenta
    {
        Efectivo = 0,
        Tarjeta = 1
    }

    public class Venta
    {
        [Key]
        public int Id_Venta { get; set; }
        public string NumeroVenta { get; set; } = "";
        public DateTime Fecha { get; set; }
        public int? Id_Cliente { get; set; }
        public Cliente? Cliente { get; set; }
        public string NombreConsumidor { get; set; } = "Consumidor Final";
        public decimal Subtotal { get; set; }
        public decimal Descuentos { get; set; }
        public decimal IVA { get; set; }
        public decimal AjusteRedondeo { get; set; }

        public decimal Total { get; set; }
        public MetodoPagoVenta MetodoPago { get; set; }
        public decimal MontoRecibido { get; set; }
        public decimal Cambio { get; set; }
        public string CreadoPor { get; set; } = "";
        public int? Id_Empleado { get; set; }
        public Empleado? Empleado { get; set; }

        public List<VentaDetalle> Detalles { get; set; } = new();
        public List<PagoVenta> Pagos { get; set; } = new();
    }
}

