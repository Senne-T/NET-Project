namespace Restaurant.ViewModels.Gebruiker
{
    public class GebruikerWachtwoordBeherenViewModel
    {
        public string Id { get; set; }

        [Required(ErrorMessage = "Vul het huidig wachtwoord in.")]
        [DataType(DataType.Password)]
        [Display(Name = "Huidig wachtwoord")]
        public string OriginalPassword { get; set; }

        [Required(ErrorMessage = "Vul een nieuw wachtwoord in.")]
        [MinLength(10, ErrorMessage = "Het wachtwoord moet minstens 10 karakters zijn.")]
        [StringLength(100, ErrorMessage = "Het wachtwoord mag maximum 100 karakters zijn.")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string NewPassword { get; set; }

        [Required(ErrorMessage = "Vul het nieuwe wachtwoord nog eens in.")]
        [Compare("NewPassword", ErrorMessage = "De wachtwoorden komen niet overeen.")]
        [DataType(DataType.Password)]
        [Display(Name = "Herhaal wachtwoord")]
        public string ConfirmPassword { get; set; }
    }
}
