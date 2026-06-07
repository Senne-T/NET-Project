namespace Restaurant.Data.Repository
{
    public interface IReservatieRepository : IGenericRepository<Reservatie>
    {
        Task<bool> IsSluitingsdagAsync(DateTime datum);
        Task<BeschikbaarheidViewModel> ControleerBeschikbaarheidAsync(DateTime datum, int tijdSlotId, int aantalPersonen);
        Task<int> BerekenAantalBeschikbareGroepenAsync(DateTime datum, int tijdSlotId, int aantalPersonen);
        Task<bool> MaakReservatieAsync(string klantId, ReservatieViewModel model);
        Task<List<int>> ZoekGeschikteTafelsAsync(DateTime datum, int tijdSlotId, int aantalPersonen);
        List<List<int>> GetMogelijkeTafelCombinaties(int aantalPersonen);
        Task<List<Reservatie>> GetReservatiesByUserIdAsync(string userId);
        Task<bool> AnnuleerReservatieAsync(int reservatieId, string userId);
        Task<bool> WijzigReservatieAsync(int reservatieId, string userId, ReservatieViewModel model);
        Task<List<Reservatie>> GetReservatiesVoorDatumEnTijdslotAsync(DateTime datum, int tijdslotId);
        Task<List<Reservatie>> GetReservatiesVoorDatumAsync(DateTime datum);
        Task<Reservatie?> GetActiveReservationTodayAsync(string userId);
        Task<List<Reservatie>> GetAllEvaluationsAsync();
        Task<Reservatie?> GetReservatieWithUserAsync(int id);
        Task<List<Reservatie>> GetAllNonEvaluatedReservationsAsync();
        Task<List<Reservatie>> GetBetaaldeReservatiesVoorDatumAsync(DateTime datum);

        // Specifiek voor betalingen - met bestellingen en prijzen
        Task<List<Reservatie>> GetReservatiesMetBestellingenVoorDatumEnTijdslotAsync(DateTime datum, int tijdslotId);
        Task<Reservatie?> GetReservatieMetBestellingenAsync(int id);

        // Nieuwe methodes voor welkomstmail scheduling
        Task<List<Reservatie>> GetReservatiesWachtendeWelkomstmailAsync(int binnenAantalDagen);
        Task<Reservatie?> FindReservatieByUserDateSlotAsync(string userId, DateTime datum, int tijdSlotId, int aantalPersonen);
        Task MarkWelkomstmailVerstuurdAsync(int reservatieId, DateTime when);
    }
}
