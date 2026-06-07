namespace Restaurant.ViewModels.Gebruikers
{
    public class GebruikersEditViewModel
    {
        public string Id { get; set; }

        public string? Voornaam { get; set; }

        public string? Achternaam { get; set; }

        [Required(ErrorMessage = "Het e-mailadres is verplicht.")]
        [EmailAddress]
        [Display(Name = "E-mailadres")]
        public string Emailadres { get; set; }

        public string? Adres { get; set; }

        public string? Huisnummer { get; set; }

        public string? Gemeente { get; set; }

        public string? Postcode { get; set; }

        [Required(ErrorMessage = "Het land is verplicht.")]
        public int LandId { get; set; }

        public List<SelectListItem>? Landen { get; set; }

        public string? RolToevoegenId { get; set; }

        public List<SelectListItem>? AlleRollen { get; set; }

        public string? RolVerwijderenId { get; set; }

        public List<SelectListItem>? HuidigeRollen { get; set; }
    }
}
