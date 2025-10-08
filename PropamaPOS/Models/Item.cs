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
        [MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [MaxLength(50)]
        public string? Codigo { get; set; }

        public bool Activo { get; set; } = true;

        public int? Id_Proveedor { get; set; }
        [ForeignKey("Id_Proveedor")]
        public Proveedor? Proveedor { get; set; }

        public ICollection<ItemPresentacion> Presentaciones { get; set; } = new List<ItemPresentacion>();
    }
}
