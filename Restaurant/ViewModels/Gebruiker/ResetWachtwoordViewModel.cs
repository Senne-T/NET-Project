namespace Restaurant.ViewModels.Gebruiker
{
    public class ResetWachtwoordViewModel
    {
        [Required]
        public string Token { get; set; } = null!;

        [Required]
        [EmailAddress]
        [Display(Name = "E-mailadres")]
        public string Emailadres { get; set; } = null!;

        [Required(ErrorMessage = "Vul een nieuw wachtwoord in.")]
        [MinLength(10, ErrorMessage = "Het wachtwoord moet minstens 10 karakters zijn.")]
        [StringLength(100, ErrorMessage = "Het wachtwoord mag maximum 100 karakters zijn.")]
        [DataType(DataType.Password)]
        [Display(Name = "Nieuw wachtwoord")]
        public string NieuwWachtwoord { get; set; } = null!;


        [Required(ErrorMessage = "Vul het wachtwoord nog eens in.")]
        [DataType(DataType.Password)]
        [Display(Name = "Bevestig wachtwoord")]
        [Compare("NieuwWachtwoord", ErrorMessage = "De wachtwoorden komen niet overeen.")]
        public string BevestigWachtwoord { get; set; } = null!;
    }
}
