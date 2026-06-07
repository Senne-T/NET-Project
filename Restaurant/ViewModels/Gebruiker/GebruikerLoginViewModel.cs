namespace Restaurant.ViewModels.Gebruiker
{
    public class GebruikerLoginViewModel
    {
        [Required(ErrorMessage = "Vul een e-mailadres in.")]
        [EmailAddress]
        [Display(Name = "E-mailadres")]
        public string Emailadres { get; set; }

        [Required(ErrorMessage = "Vul een wachtwoord in.")]
        [DataType(DataType.Password)]
        [Display(Name = "Wachtwoord")]
        public string Password { get; set; }
    }
}
