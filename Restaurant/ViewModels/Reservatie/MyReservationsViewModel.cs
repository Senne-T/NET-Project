namespace Restaurant.ViewModels.Reservatie
{
    public class MyReservationsViewModel
    {
        public List<ReservatieListViewModel> UpcomingReservations { get; set; } = new();
        public List<ReservatieListViewModel> PastReservations { get; set; } = new();
    }
}