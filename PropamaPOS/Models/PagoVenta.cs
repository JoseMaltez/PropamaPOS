using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class PagoVenta
    {
        [Key]
        public int Id_PagoVenta { get; set; }
        public int Id_Venta { get; set; }
        public Venta? Venta { get; set; }

        public MetodoPagoVenta Metodo { get; set; }
        public decimal Monto { get; set; }
        public DateTime Fecha { get; set; }
        public string? Nota { get; set; }
    }
}
