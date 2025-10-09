using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace PropamaPOS.Models.ViewModels
{
    public class CompraLineaCrearViewModel
    {
        [Required]
        public int Id_ItemPresentacion { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Debe ingresar una cantidad válida")]
        public int CantidadPresentaciones { get; set; } = 1;

        [Required]
        [Range(0.01, double.MaxValue, ErrorMessage = "Debe ingresar un precio válido")]
        public decimal PrecioCostoPorPresentacion { get; set; }
    }

    public class CompraCrearViewModel
    {
        [Required]
        public int Id_Proveedor { get; set; }

        public DateTime? Fecha { get; set; }

        public string? Nota { get; set; }

        public List<CompraLineaCrearViewModel> Lineas { get; set; } = new List<CompraLineaCrearViewModel>();
    }
}
