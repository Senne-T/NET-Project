namespace Restaurant.ViewModels.Bestellingen
{
    public class OrderListViewModel
    {
        public int BestellingId { get; set; }
        public string ProductNaam { get; set; } = string.Empty;
        public int Aantal { get; set; }
        public string? Opmerking { get; set; }
        public string Status { get; set; } = string.Empty;
        // Comma-separated tafel nummers for the reservation
        public string? TafelNummers { get; set; }
        // Numeric status id so clients can decide toggle behavior
        public int StatusId { get; set; }
    }
}
