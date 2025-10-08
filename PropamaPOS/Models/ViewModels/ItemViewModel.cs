using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ItemPresentacionViewModel
    {
        public int? Id_ItemPresentacion { get; set; }

        [Required]
        public int Id_UnidadMedida { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "La cantidad debe ser al menos 1")]
        public int Cantidad { get; set; } = 1;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "El precio debe ser mayor a 0")]
        public decimal PrecioVenta { get; set; }

        public decimal? PrecioCosto { get; set; }
    }

    public class ItemViewModel
    {
        public int Id_Item { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        public string? Codigo { get; set; }

        public int? Id_Proveedor { get; set; }

        public bool Activo { get; set; } = true;

        public List<ItemPresentacionViewModel> Presentaciones { get; set; } = new List<ItemPresentacionViewModel>();
    }
}
