namespace Restaurant.ViewModels.Mail
{
    public class MailViewModel
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Naam is verplicht")]
        [Display(Name = "Naam")]
        [StringLength(100, ErrorMessage = "Naam mag maximaal 100 karakters bevatten")]
        public string? Naam { get; set; }

        [Required(ErrorMessage = "Onderwerp is verplicht")]
        [Display(Name = "Onderwerp")]
        [StringLength(200, ErrorMessage = "Onderwerp mag maximaal 200 karakters bevatten")]
        public string? Onderwerp { get; set; }

        [Required(ErrorMessage = "Inhoud is verplicht")]
        [Display(Name = "Inhoud")]
        public string? Body { get; set; }
    }
}
