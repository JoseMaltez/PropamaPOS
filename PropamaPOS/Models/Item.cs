using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace PropamaPOS.Models
{
    public class Item
    {
        [Key]
        public int Id_Item { get; set; }

        [Required]
        [StringLength(20)]
        public string Codigo { get; set; } = string.Empty;

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }
        public bool IsServicio { get; set; } = false;

        public int Stock { get; set; } = 0;
        public int? StockMinimo { get; set; } = null;

        [Column(TypeName = "decimal(18,4)")]
        public decimal CostoPromedioUnidad { get; set; } = 0m;

        public bool Activo { get; set; } = true;

        public ICollection<ItemPresentacion> Presentaciones { get; set; } = new List<ItemPresentacion>();
        public ICollection<ServicioComponente>? ServicioComponentes { get; set; }


        public int? Id_Categoria { get; set; }
        public Categoria? Categoria { get; set; }
    }
}
