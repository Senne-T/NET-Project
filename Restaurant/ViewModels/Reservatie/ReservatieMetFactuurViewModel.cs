namespace Restaurant.ViewModels.Reservatie
{
    public class ReservatieMetFactuurViewModel
    {
        public int ReservatieId { get; set; }
        public string KlantNaam { get; set; } = string.Empty;
        public DateTime? Datum { get; set; }
        public string Tijdslot { get; set; } = string.Empty;
        public string Tafels { get; set; } = string.Empty;
        public int AantalPersonen { get; set; }
        public decimal TotaalBedrag { get; set; }
        public bool Betaald { get; set; }
        public bool IsAanwezig { get; set; }
    }
}
