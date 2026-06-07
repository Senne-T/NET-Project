using System.ComponentModel.DataAnnotations;

namespace Restaurant.Models
{
    public class Mail
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string? Naam { get; set; }

        [Required]
        public string? Onderwerp { get; set; }

        [Required]
        public string? Body { get; set; }
    }
}
