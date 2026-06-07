namespace Restaurant.Data.Repository
{
    public class TijdslotRepository : ITijdslotRepository
    {
        private readonly RestaurantContext _context;

        public TijdslotRepository(RestaurantContext context)
        {
            _context = context;
        }

        public async Task<List<Tijdslot>> GetActiveTijdslotsAsync()
        {
            return await _context.Tijdslots
                .Where(ts => ts.Actief)
                .OrderBy(ts => ts.Id)
                .ToListAsync();
        }
    }
}
