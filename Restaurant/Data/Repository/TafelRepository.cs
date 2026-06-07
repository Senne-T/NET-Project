namespace Restaurant.Data.Repository
{
    public class TafelRepository : ITafelRepository
    {
        private readonly RestaurantContext _context;

        public TafelRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<List<Tafel>> GetActiveTafelsAsync()
        {
            return await _context.Tafels
                .Where(t => t.Actief)
                .OrderBy(t => t.Id)
                .ToListAsync();
        }

        public async Task<List<Tafel>> GetAllTafelsAsync()
        {
            return await _context.Tafels
                .OrderBy(t => t.TafelNummer)
                .ToListAsync();
        }

        public async Task<Tafel?> GetByIdAsync(int id)
        {
            return await _context.Tafels
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task AddAsync(Tafel tafel)
        {
            await _context.Tafels.AddAsync(tafel);
        }

        public async Task UpdateAsync(Tafel tafel)
        {
            _context.Tafels.Update(tafel);
        }

        public async Task DeleteAsync(int id)
        {
            var tafel = await _context.Tafels.FindAsync(id);
            if (tafel != null)
            {
                _context.Tafels.Remove(tafel);
            }
        }
    }
}
