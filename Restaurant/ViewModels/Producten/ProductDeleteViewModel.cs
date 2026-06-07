
namespace Restaurant.ViewModels
{
    public class ProductDeleteViewModel
    {
        public int Id { get; set; }
        public string Naam { get; set; }
        public string CategorieNaam { get; set; }
        public string AllergenenInfo { get; set; }
        public bool Actief { get; set; }
        public decimal Prijs { get; set; }
    }
}