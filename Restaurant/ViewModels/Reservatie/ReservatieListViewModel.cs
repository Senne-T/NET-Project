namespace Restaurant.ViewModels.Reservatie
{
    public class ReservatieListViewModel
    {
        public int Id { get; set; }
        public DateTime? Datum { get; set; }
        public string? TijdSlotNaam { get; set; }
        public int AantalPersonen { get; set; }
        public string? Opmerking { get; set; }
        public string? TafelNummers { get; set; } // Comma-separated table numbers
        public bool CanCancel { get; set; }
        public bool Bestaald { get; set; }
        public int EvaluatieAantalSterren { get; set; }
        public bool IsAanwezig { get; set; }
    }
}