namespace Restaurant.Data.Repository
{
    public interface ITafelRepository
    {
        Task<List<Tafel>> GetActiveTafelsAsync();
        Task<List<Tafel>> GetAllTafelsAsync();
        Task<Tafel?> GetByIdAsync(int id);
        Task AddAsync(Tafel tafel);
        Task UpdateAsync(Tafel tafel);
        Task DeleteAsync(int id);
    }
}
