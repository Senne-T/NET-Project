namespace Restaurant.ViewModels.Reservatie
{
    public class TijdslotBeschikbaarheidViewModel
    {
        public int TijdSlotId { get; set; }
        public string Naam { get; set; } = string.Empty;
        public bool IsBeschikbaar { get; set; }
        public int BeschikbarePlaatsen { get; set; }
    }
}
