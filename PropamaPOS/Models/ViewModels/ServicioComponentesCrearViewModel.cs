using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class ServicioComponenteLineaViewModel
    {
        [Required]
        public int Id_Item { get; set; }
        [Required]
        [Range(0.0001, double.MaxValue)]
        public decimal CantidadPorServicio { get; set; }
    }

    public class ServicioComponentesCrearViewModel
    {
        [Required]
        public int Id_Servicio { get; set; }

        public List<ServicioComponenteLineaViewModel> Lineas { get; set; } = new();
    }
}
