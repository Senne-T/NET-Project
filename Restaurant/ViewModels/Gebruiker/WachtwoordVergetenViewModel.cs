namespace Restaurant.ViewModels.Gebruiker
{
    public class WachtwoordVergetenViewModel
    {
        [Required(ErrorMessage = "Vul een e-mailadres in.")]
        [EmailAddress]
        [Display(Name = "E-mailadres")]
        public string Emailadres { get; set; } = null!;
    }
}
