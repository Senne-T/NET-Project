using System.ComponentModel.DataAnnotations.Schema;

namespace Restaurant.Models
{
    public class CustomUser : IdentityUser
    {
        public string? Voornaam { get; set; }
        public string? Achternaam { get; set; }
        public string? Adres { get; set; }
        public string? Huisnummer { get; set; }
        public string? Postcode { get; set; }
        public string? Gemeente { get; set; }
        public bool Actief { get; set; }

        [ForeignKey("Land")]
        public int LandId { get; set; }

        // Navigation properties
        public virtual Land Land { get; set; }
        public virtual ICollection<Reservatie> Reservaties { get; set; } = new List<Reservatie>();
    }
}
