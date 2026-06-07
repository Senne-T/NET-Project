namespace Restaurant.ViewModels.Bestelling
{
    public class BestellingMenuItemViewModel
    {
        public int ItemId { get; set; } 
        public bool IsDrank { get; set; }  

        public string Naam { get; set; } = string.Empty;
        public string CategorieNaam { get; set; } = string.Empty;

        public decimal Prijs { get; set; }

        public int Aantal { get; set; }
    }
}
