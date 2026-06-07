using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels
{
    public class ProductCreateViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naam is verplicht")]
        [MaxLength(100)]
        public string Naam { get; set; }

        [Required(ErrorMessage = "Beschrijving is verplicht")]
        [MaxLength(500)]
        public string Beschrijving { get; set; }

        [Required(ErrorMessage = "Allergen Info is verplicht (schrijf 'Geen' als er geen allergen zijn)")]
        [MaxLength(500)]
        public string AllergenenInfo { get; set; }

        [Required]
        public int CategorieId { get; set; }

        [Required]
        [Range(0.01, 999.99, ErrorMessage = "Prijs moet tussen €0,01 en €999,99 liggen")]
        public decimal Prijs { get; set; }

        [Required]
        public bool Actief { get; set; }
        [Required]
        public bool IsSuggestie { get; set; }

        public IEnumerable<SelectListItem>? Categorieën { get; set; }
    }
}
