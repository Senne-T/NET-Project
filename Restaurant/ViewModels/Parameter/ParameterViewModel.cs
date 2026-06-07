using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels.Parameter
{
    public class ParameterViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naam is verplicht")]
        [StringLength(100)]
        public string Naam { get; set; } = string.Empty;

        [Required(ErrorMessage = "Waarde is verplicht")]
        [StringLength(500)]
        public string Waarde { get; set; } = string.Empty;
    }
}
