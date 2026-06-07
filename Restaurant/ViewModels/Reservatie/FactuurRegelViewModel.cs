namespace Restaurant.ViewModels.Reservatie
{
    public class FactuurRegelViewModel
    {
        public string ProductNaam { get; set; } = string.Empty;
        public int Aantal { get; set; }
        public decimal Prijs { get; set; }
        public decimal Subtotaal => Aantal * Prijs;
        public int BestellingId { get; set; }
        public bool Geannuleerd { get; set; }
        
        // Voor gegroepeerde items: alle bestelling IDs in deze groep
        public List<int> AlleBestellingIds { get; set; } = new List<int>();
    }
}
