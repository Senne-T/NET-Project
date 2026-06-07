namespace Restaurant.ViewModels.Bestelling
{
    public class BestellingRegelViewModel
    {
        public int ItemId { get; set; }
        public bool IsDrank { get; set; }

        public string Naam { get; set; } = string.Empty;
        public int Aantal { get; set; }
        public decimal Stukprijs { get; set; }

        public decimal Totaal => Aantal * Stukprijs;
    }
}
