namespace Restaurant.ViewModels.Tafel
{
    public class ReservatieOverzichtViewModel
    {
        public int Id { get; set; }
        public DateTime Datum { get; set; }
        public int TijdSlotId { get; set; }
        public string TijdslotNaam { get; set; } = string.Empty;
        public int AantalPersonen { get; set; }
        public bool IsAanwezig { get; set; }
        public List<int> TafelIds { get; set; } = new List<int>();
        public string KlantNaam { get; set; } = string.Empty;
        public string TafelNummers { get; set; } = string.Empty;
    }
}
