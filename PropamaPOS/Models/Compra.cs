// PropamaPOS/Models/Compra.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class Compra
    {
        [Key]
        public int Id_Compra { get; set; }

        [Required]
        [StringLength(15)]
        public string NumeroCompra { get; set; } = string.Empty;
        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public int Id_Proveedor { get; set; }
        [ForeignKey("Id_Proveedor")]
        public Proveedor Proveedor { get; set; }

        public int? Id_Empleado { get; set; }
        [ForeignKey("Id_Empleado")]
        public Empleado? Empleado { get; set; }

        [MaxLength(150)]
        public string CreadoPor { get; set; }

        [Column(TypeName = "decimal(18,2)")]
        public decimal Total { get; set; }

        public string? Nota { get; set; }

        public ICollection<CompraDetalle> Detalles { get; set; } = new List<CompraDetalle>();
    }
}
