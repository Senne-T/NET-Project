namespace Restaurant.ViewModels.Tafel
{
    public class TafelToewijzingViewModel
    {
        public int ReservatieId { get; set; }
        public string KlantNaam { get; set; } = string.Empty;
        public int AantalPersonen { get; set; }
        public DateTime Datum { get; set; }
        public string TijdslotNaam { get; set; } = string.Empty;
        public string GereserveerdeTafels { get; set; } = string.Empty;
        public List<TafelOptieViewModel> BeschikbareTafels { get; set; } = new();
    }

    public class TafelOptieViewModel
    {
        public int TafelId { get; set; }
        public string? TafelNummer { get; set; }
        public int AantalPersonen { get; set; }
        public int MinAantalPersonen { get; set; }
        public bool IsBezet { get; set; }
        public bool IsGereserveerd { get; set; }
    }
}
