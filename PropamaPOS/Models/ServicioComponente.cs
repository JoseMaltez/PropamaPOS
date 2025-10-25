using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace PropamaPOS.Models
{
    public class ServicioComponente
    {
        [Key]
        public int Id_ServicioComponente { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un servicio.")]
        [Display(Name = "Servicio")]
        public int Id_Servicio { get; set; }

        [Required(ErrorMessage = "Debe seleccionar un insumo.")]
        [Display(Name = "Insumo (producto consumido)")]
        public int Id_Item { get; set; }

        [Required(ErrorMessage = "Debe ingresar una cantidad válida.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "La cantidad debe ser mayor que 0.")]
        [Display(Name = "Cantidad por servicio")]
        public decimal CantidadPorServicio { get; set; }

        [ValidateNever]
        public Item? Servicio { get; set; }

        [ValidateNever]
        public Item? ItemConsumido { get; set; }
    }
}
