using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public enum TipoMovimientoAjuste
    {
        Entrada = 1,
        Salida = 2,
    }

    public class AjusteInventario
    {
        [Key]
        public int Id_Ajuste { get; set; }

        public DateTime Fecha { get; set; } = DateTime.UtcNow;

        public int? Id_Empleado { get; set; }
        [ForeignKey("Id_Empleado")]
        public Empleado? Empleado { get; set; }

        public TipoMovimientoAjuste Tipo { get; set; }

        [Required]
        [MaxLength(200)]
        public string Motivo { get; set; }

        public string? Observaciones { get; set; }

        [MaxLength(150)]
        public string CreadoPor { get; set; }

        public ICollection<AjusteInventarioDetalle> Detalles { get; set; } = new List<AjusteInventarioDetalle>();
    }
}
