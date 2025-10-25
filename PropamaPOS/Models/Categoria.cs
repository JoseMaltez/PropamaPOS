using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models
{
    public class Categoria
    {
        [Key]
        public int Id_Categoria { get; set; }

        [Required(ErrorMessage = "El nombre de la categoría es obligatorio")]
        [MaxLength(100)]
        public string Nombre { get; set; }

        [MaxLength(250)]
        public string? Descripcion { get; set; }

        public ICollection<Item>? Items { get; set; }
    }
}
