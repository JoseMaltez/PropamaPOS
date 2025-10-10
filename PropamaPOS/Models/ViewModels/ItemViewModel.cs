// PropamaPOS/Models/ViewModels/ItemViewModel.cs
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ItemPresentacionViewModel
    {
        public int? Id_ItemPresentacion { get; set; }

        [Required]
        public int Id_UnidadMedida { get; set; }

        [Range(1, int.MaxValue)]
        public int Cantidad { get; set; } = 1;
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

        // Quitado Id_Proveedor: relación con proveedores se maneja por ItemProveedor
        public List<ItemPresentacionViewModel> Presentaciones { get; set; } = new();
        public bool Activo { get; set; } = true;
    }
}
