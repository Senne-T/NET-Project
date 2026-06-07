namespace Restaurant.Data.Repository
{
    public interface ITijdslotRepository
    {
        Task<List<Tijdslot>> GetActiveTijdslotsAsync();
    }
}
