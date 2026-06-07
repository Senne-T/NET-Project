using Restaurant.ViewModels.Land;

namespace Restaurant.ViewModels.Gebruiker
{
    public class GebruikerDeleteViewModel
    {
        public string Id { get; set; }

        public string? Voornaam { get; set; }

        public string? Achternaam { get; set; }

        public string? Adres { get; set; }

        public string? Huisnummer { get; set; }

        public string? Gemeente { get; set; }

        public string? Postcode { get; set; }

        public LandViewModel Land { get; set; }

        public string Emailadres { get; set; }
    }
}
