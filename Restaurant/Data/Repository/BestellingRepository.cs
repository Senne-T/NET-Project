using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Restaurant.Models;
using Restaurant.ViewModels.Bestelling;
using Microsoft.Extensions.Logging;

namespace Restaurant.Data.Repository
{
    public class BestellingRepository : GenericRepository<Bestelling>, IBestellingRepository
    {
        private readonly ILogger _logger;

        public BestellingRepository(RestaurantContext context, ILogger logger) : base(context)
        {
            _logger = logger;
        }

        public async Task<List<BestellingMenuItemViewModel>> GetMenuItemsAsync()
        {
            // Laad producten met categorie�n en prijzen
            var producten = await _context.Set<Product>()
                .Include(p => p.Categorie)
                    .ThenInclude(c => c.Type)
                .Include(p => p.PrijsProducten)
                .ToListAsync();

            var menuItems = new List<BestellingMenuItemViewModel>();
            foreach (var p in producten)
            {
                var currentPrijs = p.PrijsProducten.OrderByDescending(pp => pp.DatumVanaf ?? DateTime.MinValue).FirstOrDefault()?.Prijs ?? 0m;
                menuItems.Add(new BestellingMenuItemViewModel
                {
                    ItemId = p.Id,
                    Naam = p.Naam ?? string.Empty,
                    CategorieNaam = p.Categorie?.Naam ?? string.Empty,
                    Prijs = currentPrijs,
                    IsDrank = p.Categorie?.Type?.Naam?.ToLower() == "drank",
                    Aantal = 0
                });
            }

            return menuItems;
        }

        public async Task<Reservatie?> GetActiveReservationForUserAsync(string userId)
        {
            var today = DateTime.Today;
            return await _context.Set<Reservatie>()
                .FirstOrDefaultAsync(r => r.KlantId == userId && r.Datum == today && r.IsAanwezig && !r.Bestaald);
        }

        public async Task SaveBestellingenAsync(List<BestellingMenuItemViewModel> items, int reservatieId)
        {
            // Controleer of reservatie bestaat
            var reservatieExists = await _context.Set<Reservatie>().FindAsync(reservatieId) != null;

            if (!reservatieExists)
            {
                throw new InvalidOperationException("Reservatie bestaat niet.");
            }

            foreach (var item in items.Where(i => i.Aantal > 0))
            {
                var bestelling = new Bestelling
                {
                    ReservatieId = reservatieId,
                    ProductId = item.ItemId,
                    Aantal = item.Aantal,
                    TijdstipBestelling = DateTime.Now,
                    // New orders should start with status "Niet Gestart" (id = 5)
                    StatusId = 5
                };

                await AddAsync(bestelling);
                _logger.LogInformation($"Bestelling opgeslagen: ProductId {item.ItemId}, Aantal {item.Aantal}");
            }

            await _context.SaveChangesAsync();
        }

        public async Task<BestellingGeluktViewModel> GetBestellingOverzichtFromDbAsync(int reservatieId)
        {
            var bestellingen = await Find(b => b.ReservatieId == reservatieId && b.TijdstipBestelling > DateTime.Now.AddMinutes(-5), b => b.Product, b => b.Product.PrijsProducten);

            var vm = new BestellingGeluktViewModel
            {
                ReservatieId = reservatieId,
                Bestellingen = bestellingen.Where(b => b.Aantal > 0).Select(b => new BestellingOverzichtItemViewModel
                {
                    ProductNaam = b.Product?.Naam ?? "Onbekend",
                    Aantal = b.Aantal,
                    Prijs = b.Product?.PrijsProducten?.OrderByDescending(p => p.DatumVanaf ?? DateTime.MinValue).FirstOrDefault()?.Prijs ?? 0,
                    Subtotaal = (b.Product?.PrijsProducten?.OrderByDescending(p => p.DatumVanaf ?? DateTime.MinValue).FirstOrDefault()?.Prijs ?? 0) * b.Aantal
                }).ToList(),
                Totaal = bestellingen.Where(b => b.Aantal > 0).Sum(b => (b.Product?.PrijsProducten?.OrderByDescending(p => p.DatumVanaf ?? DateTime.MinValue).FirstOrDefault()?.Prijs ?? 0) * b.Aantal)
            };

            return vm;
        }

        // Additional helper methods used by controllers
        public async Task<IEnumerable<Bestelling>> GetOpenBestellingenAsync()
        {
            return await _context.Bestellingen
                .Include(b => b.Product)
                    .ThenInclude(p => p.Categorie)
                        .ThenInclude(c => c.Type)
                .Include(b => b.Status)
                .Include(b => b.Reservatie)
                    .ThenInclude(r => r.Tafellijsten)
                        .ThenInclude(tl => tl.Tafel)
                .Where(b => b.Status.Naam != "Geserveerd" 
                    && b.Status.Naam != "Geannuleerd"
                    && b.Product.Categorie.Type.Naam != "Dranken")  // Exclude drinks - those go to Ober
                .ToListAsync();
        }

        public async Task<IEnumerable<Bestelling>> GetByStatusAsync(int statusId)
        {
            return await _context.Bestellingen
                .Include(b => b.Product)
                    .ThenInclude(p => p.Categorie)
                        .ThenInclude(c => c.Type)
                .Include(b => b.Status)
                .Include(b => b.Reservatie)
                    .ThenInclude(r => r.Tafellijsten)
                        .ThenInclude(tl => tl.Tafel)
                .Where(b => b.StatusId == statusId)
                .ToListAsync();
        }

        // Get all drink orders that are not served yet (for Ober)
        public async Task<IEnumerable<Bestelling>> GetOpenDrankenAsync()
        {
            return await _context.Bestellingen
                .Include(b => b.Product)
                    .ThenInclude(p => p.Categorie)
                        .ThenInclude(c => c.Type)
                .Include(b => b.Status)
                .Include(b => b.Reservatie)
                    .ThenInclude(r => r.Tafellijsten)
                        .ThenInclude(tl => tl.Tafel)
                .Where(b => b.Status.Naam != "Geserveerd" 
                    && b.Status.Naam != "Geannuleerd"
                    && b.Status.Naam != "Klaar"  // Exclude Klaar - those come from GetKlaarVoorOberAsync
                    && b.Product.Categorie.Type.Naam == "Dranken")  // Only drinks for Ober
                .OrderBy(b => b.TijdstipBestelling)
                .ToListAsync();
        }

        // Get all items ready to serve (Klaar status) regardless of type (for Ober)
        public async Task<IEnumerable<Bestelling>> GetKlaarVoorOberAsync()
        {
            return await _context.Bestellingen
                .Include(b => b.Product)
                    .ThenInclude(p => p.Categorie)
                        .ThenInclude(c => c.Type)
                .Include(b => b.Status)
                .Include(b => b.Reservatie)
                    .ThenInclude(r => r.Tafellijsten)
                        .ThenInclude(tl => tl.Tafel)
                .Where(b => b.Status.Naam == "Klaar")  // Ready to serve
                .OrderBy(b => b.TijdstipBestelling)
                .ToListAsync();
        }

        public async Task<Bestelling?> GetByIdAsync(int id)
        {
            return await _context.Bestellingen
                .Include(b => b.Product)
                .Include(b => b.Status)
                .Include(b => b.Reservatie)
                    .ThenInclude(r => r.Tafellijsten)
                        .ThenInclude(tl => tl.Tafel)
                .FirstOrDefaultAsync(b => b.Id == id);
        }

        public async Task UpdateStatusAsync(int bestellingId, int statusId)
        {
            var b = await _context.Bestellingen.FirstOrDefaultAsync(x => x.Id == bestellingId);
            if (b == null) return;
            b.StatusId = statusId;
            await _context.SaveChangesAsync();
        }

        public void Update(Bestelling bestelling)
        {
            _context.Bestellingen.Update(bestelling);
        }
    }
}