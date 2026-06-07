using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels.Parameter
{
    /// <summary>
    /// Vereenvoudigd ViewModel voor individuele parameters
    /// </summary>
    public class ParametersViewModel
    {
        [Display(Name = "Parameter Naam")]
        public string Naam { get; set; } = string.Empty;

        [Display(Name = "Parameter Waarde")]
        public string Waarde { get; set; } = string.Empty;
    }
}
