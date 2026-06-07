namespace Restaurant.ViewModels.Gebruikers
{
    public class GebruikersViewModel
    {
        public string Id { get; set; }

        public IList<string>? Rollen { get; set; }

        public string? Voornaam { get; set; }

        public string? Achternaam { get; set; }

        public string Emailadres { get; set; }

        public bool Actief { get; set; }
    }
}
