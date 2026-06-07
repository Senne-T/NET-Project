using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels.Tafel
{
    public class TafelCreateViewModel
    {
        [Required(ErrorMessage = "Tafelnummer is verplicht")]
        [MaxLength(50)]
        public string? TafelNummer { get; set; }

        [Required(ErrorMessage = "Aantal personen is verplicht")]
        [Range(1, 50, ErrorMessage = "Aantal personen moet tussen 1 en 50 liggen")]
        public int AantalPersonen { get; set; }

        [Required(ErrorMessage = "Minimum aantal personen is verplicht")]
        [Range(1, 50, ErrorMessage = "Minimum aantal personen moet tussen 1 en 50 liggen")]
        public int MinAantalPersonen { get; set; }

        [Required]
        public bool Actief { get; set; } = true;

        [MaxLength(200)]
        public string? QrBarcode { get; set; }
    }
}
