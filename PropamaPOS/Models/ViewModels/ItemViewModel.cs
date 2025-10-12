// PropamaPOS/Models/ViewModels/ItemViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ItemPresentacionViewModel
    {
        public int? Id_ItemPresentacion { get; set; }
        public int Id_UnidadMedida { get; set; }
        public int Cantidad { get; set; } = 1;

        // Para servicios o para edición manual de precio
        [DataType(DataType.Currency)]
        public decimal? PrecioVenta { get; set; }
    }

    public class ItemViewModel
    {
        public int? Id_Item { get; set; }

        [Required]
        [MaxLength(150)]
        public string Nombre { get; set; }

        [MaxLength(300)]
        public string? Descripcion { get; set; }

        [MaxLength(50)]
        public string? Codigo { get; set; }

        public bool Activo { get; set; } = true;

        public bool IsServicio { get; set; } = false;

        public int? Id_Categoria { get; set; }

        public List<ItemPresentacionViewModel> Presentaciones { get; set; } = new();
    }
}
