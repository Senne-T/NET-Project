namespace Restaurant.ViewModels.Reservatie
{
    public class BeschikbaarheidViewModel
    {
        public DateTime Datum { get; set; }
        public int TijdSlotId { get; set; }
        public int AantalPersonen { get; set; }
        public bool IsBeschikbaar { get; set; }
        public string Bericht { get; set; } = string.Empty;
        public List<TijdslotBeschikbaarheidViewModel> TijdSlotBeschikbaarheid { get; set; } = new();
    }
}
