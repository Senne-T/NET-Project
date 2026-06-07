using Restaurant.Models;

namespace Restaurant.Data.Repository
{
    public interface IParameterRepository : IGenericRepository<Parameter>
    {
        /// <summary>
        /// Zoekt een parameter op basis van de naam
        /// </summary>
        Task<Parameter?> FindByNameAsync(string naam);
        
        /// <summary>
        /// Haalt de waarde op van een parameter op basis van de naam
        /// </summary>
        Task<string?> GetValueByNameAsync(string naam);
    }
}
