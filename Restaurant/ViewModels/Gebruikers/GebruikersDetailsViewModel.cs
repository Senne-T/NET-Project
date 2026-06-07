namespace Restaurant.ViewModels.Gebruikers
{
    public class GebruikersDetailsViewModel
    {
        public string Id { get; set; }

        public string? Voornaam { get; set; }

        public string? Achternaam { get; set; }

        public string Emailadres { get; set; }

        public string? Adres { get; set; }

        public string? Huisnummer { get; set; }

        public string? Gemeente { get; set; }

        public string? Postcode { get; set; }

        public int LandId { get; set; }

        public string? Land { get; set; }

        public IList<string>? Rollen { get; set; }
    }
}
