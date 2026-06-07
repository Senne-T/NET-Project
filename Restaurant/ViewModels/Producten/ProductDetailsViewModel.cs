using System.ComponentModel.DataAnnotations;

namespace Restaurant.ViewModels
{
    public class ProductDetailsViewModel
    {
        public int Id { get; set; }

        [Required]
        public string Naam { get; set; }

        [Required]
        public string Beschrijving { get; set; }

        public string AllergenenInfo { get; set; }

        // Categorie info in klare taal
        public int CategorieId { get; set; }
        public string CategorieNaam { get; set; }

        // Actuele prijs (laatste prijsrecord)
        public decimal Prijs { get; set; }

        public bool Actief { get; set; }
        public bool IsSuggestie { get; set; }
    }
}
