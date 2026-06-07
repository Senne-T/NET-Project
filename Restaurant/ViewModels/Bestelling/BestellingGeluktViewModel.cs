namespace Restaurant.ViewModels.Bestelling
{
    public class BestellingGeluktViewModel
    {
        public int ReservatieId { get; set; }
        public List<BestellingOverzichtItemViewModel> Bestellingen { get; set; } = new();
        public decimal Totaal { get; set; }
    }

    public class BestellingOverzichtItemViewModel
    {
        public string ProductNaam { get; set; } = string.Empty;
        public int Aantal { get; set; }
        public decimal Prijs { get; set; }
        public decimal Subtotaal { get; set; }
    }
}