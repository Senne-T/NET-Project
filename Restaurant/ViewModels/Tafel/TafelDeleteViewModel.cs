namespace Restaurant.ViewModels.Tafel
{
    public class TafelDeleteViewModel
    {
        public int Id { get; set; }
        public string? TafelNummer { get; set; }
        public int AantalPersonen { get; set; }
        public int MinAantalPersonen { get; set; }
        public bool Actief { get; set; }
    }
}
