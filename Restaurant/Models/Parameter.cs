namespace Restaurant.Models
{
    public class Parameter
    {
        [Key]
        public int Id { get; set; }
        public string? Naam { get; set; }
        public string? Waarde { get; set; }
    }
}
