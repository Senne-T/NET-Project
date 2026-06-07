namespace Restaurant.ViewModels.Gebruikers
{
    public class GebruikersListViewModel
    {
        public List<GebruikersViewModel> Gebruikers { get; set; }

        public string? Search { get; set; }
        public bool? Actief { get; set; }
        public string? Type { get; set; }
    }
}
