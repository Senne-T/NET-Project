using Microsoft.EntityFrameworkCore;
using Restaurant.Models;

namespace Restaurant.Data.Repository
{
    public class ParameterRepository : GenericRepository<Parameter>, IParameterRepository
    {
        public ParameterRepository(RestaurantContext context) : base(context)
        {
        }

        /// <summary>
        /// Zoekt een parameter op basis van de naam
        /// </summary>
        public async Task<Parameter?> FindByNameAsync(string naam)
        {
            return await _context.Set<Parameter>()
                .FirstOrDefaultAsync(p => p.Naam == naam);
        }

        /// <summary>
        /// Haalt de waarde op van een parameter op basis van de naam
        /// </summary>
        public async Task<string?> GetValueByNameAsync(string naam)
        {
            var parameter = await FindByNameAsync(naam);
            return parameter?.Waarde;
        }
    }
}
