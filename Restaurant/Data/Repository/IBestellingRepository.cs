using System.Collections.Generic;
using System.Threading.Tasks;
using Restaurant.Models;
using Restaurant.ViewModels.Bestelling;

namespace Restaurant.Data.Repository
{
    public interface IBestellingRepository : IGenericRepository<Bestelling>
    {
        Task<List<BestellingMenuItemViewModel>> GetMenuItemsAsync();
        Task<Reservatie?> GetActiveReservationForUserAsync(string userId);
        Task SaveBestellingenAsync(List<BestellingMenuItemViewModel> items, int reservatieId);
        Task<BestellingGeluktViewModel> GetBestellingOverzichtFromDbAsync(int reservatieId);

        // Extended signatures implemented in BestellingRepository
        Task<IEnumerable<Bestelling>> GetOpenBestellingenAsync();
        Task<IEnumerable<Bestelling>> GetByStatusAsync(int statusId);
        Task<IEnumerable<Bestelling>> GetOpenDrankenAsync();
        Task<IEnumerable<Bestelling>> GetKlaarVoorOberAsync();
        Task<Bestelling?> GetByIdAsync(int id);
        Task UpdateStatusAsync(int bestellingId, int statusId);
        void Update(Bestelling bestelling);
    }
}