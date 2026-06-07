namespace Restaurant.ViewModels.Gebruiker
{
    public class GebruikerRegisterViewModel
    {
        public string? Voornaam { get; set; }

        public string? Achternaam { get; set; }

        public string? Adres { get; set;  }

        public string? Huisnummer { get; set; }

        public string? Gemeente { get; set; }

        public string? Postcode { get; set; }

        [Required(ErrorMessage = "Het land is verplicht.")]
        public int LandId { get; set; }

        [Required(ErrorMessage = "Het e-mailadres is verplicht.")]
        [EmailAddress]
        [Display(Name = "E-mailadres")]
        public string Emailadres { get; set; }

        [Required(ErrorMessage = "Het wachtwoord is verplicht.")]
        [MinLength(10, ErrorMessage = "Het wachtwoord moet minstens 10 karakters zijn.")]
        [StringLength(100, ErrorMessage = "Het wachtwoord mag maximum 100 karakters zijn.")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Vul het wachtwoord nog eens in.")]
        [Compare("Password", ErrorMessage = "De wachtwoorden komen niet overeen.")]
        [DataType(DataType.Password)]
        [Display(Name = "Herhaal wachtwoord")]
        public string ConfirmPassword { get; set; }

        public List<SelectListItem>? Landen { get; set; }
    }
}
