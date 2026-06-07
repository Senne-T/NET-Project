namespace Restaurant.ViewModels.Enquete
{
    public class EnqueteViewModel
    {
        public int Id { get; set; }
        public DateTime? Date { get; set; }
        public string? CustomerName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public int Score { get; set; }
        public string? Comments { get; set; }
    }
}