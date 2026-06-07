namespace Restaurant.ViewModels.Mail
{
    public class TestMailViewModel
    {
        [Required(ErrorMessage = "Template ID is verplicht")]
        public int TemplateId { get; set; }
        
        public string? TemplateNaam { get; set; }
        
        [Required(ErrorMessage = "E-mailadres is verplicht")]
        [EmailAddress(ErrorMessage = "Ongeldig e-mailadres")]
        [Display(Name = "Test E-mailadres")]
        public string? TestEmail { get; set; }
    }
}
