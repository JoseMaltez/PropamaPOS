// PropamaPOS/Models/ViewModels/AjusteCrearViewModel.cs
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using PropamaPOS.Models;

namespace PropamaPOS.Models.ViewModels
{
    public class AjusteLineaViewModel
    {
        public int? Id_Item { get; set; }
        public int? Id_ItemPresentacion { get; set; }

        [Range(1, int.MaxValue)]
        public int CantidadPresentaciones { get; set; } = 1;

        public string? Nota { get; set; }
    }

    public class AjusteCrearViewModel
    {
        public DateTime? Fecha { get; set; }

        public TipoMovimientoAjuste Tipo { get; set; } = TipoMovimientoAjuste.Entrada;

        [Required]
        [MaxLength(200)]
        public string Motivo { get; set; }

        public string? Observaciones { get; set; }

        public List<AjusteLineaViewModel> Lineas { get; set; } = new();
    }
}

