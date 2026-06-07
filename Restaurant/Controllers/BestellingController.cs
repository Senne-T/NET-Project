using Restaurant.ViewModels.Bestelling;
using System.Security.Claims;
using System.Text.Json;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Klant)]
    public class BestellingController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly ILogger<BestellingController> _logger;

        public BestellingController(IUnitOfWork unitOfWork, ILogger<BestellingController> logger)
        {
            _unitOfWork = unitOfWork;
            _logger = logger;
        }

        // GET: Bestelling/Menu
        // Toont het bestelmenu voor ingelogde gebruikers met actieve reservatie.
        [HttpGet]
        public async Task<IActionResult> Menu(int? reservatieId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return View("NoAccess");
            }

            var reservatie = await _unitOfWork.BestellingRepository.GetActiveReservationForUserAsync(userId);

            if (reservatie == null)
            {
                return View("NoAccess");
            }

            var vm = new BestellingMenuViewModel
            {
                ReservatieId = reservatie.Id,
                Items = await _unitOfWork.BestellingRepository.GetMenuItemsAsync()
            };

            // Herstel eerder geselecteerde aantallen uit TempData alleen na terugkeer van bevestiging
            var json = TempData["GeselecteerdeItems"] as string;
            _logger.LogInformation($"Menu GET: Json aanwezig = {json != null}");
            if (json != null)
            {
                var geselecteerd = JsonSerializer.Deserialize<List<BestellingMenuItemViewModel>>(json);
                if (geselecteerd != null)
                {
                    _logger.LogInformation($"Herstel {geselecteerd.Count} items");
                    foreach (var item in geselecteerd)
                    {
                        var menuItem = vm.Items.FirstOrDefault(i => i.ItemId == item.ItemId);
                        _logger.LogInformation($"Update item {item.ItemId} naar {item.Aantal}, gevonden: {menuItem != null}");
                        if (menuItem != null)
                        {
                            menuItem.Aantal = item.Aantal;
                            _logger.LogInformation($"Updated {item.ItemId} to {item.Aantal}");
                        }
                    }
                    _logger.LogInformation($"Na update: {string.Join(", ", vm.Items.Where(i => i.Aantal > 0).Select(i => $"{i.ItemId}:{i.Aantal}"))}");
                }
                TempData.Remove("GeselecteerdeItems");
                _logger.LogInformation("TempData removed after restore");
            } else {
                _logger.LogInformation("Geen herstel gedaan");
            }

            // Voorkom caching van de pagina om problemen bij teruggaan te vermijden
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            return View(vm);
        }

        // POST: Bestelling/Menu
        // Verwerkt selectie en toont bevestiging.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Menu(BestellingMenuViewModel vm)
        {
            var geselecteerd = vm.Items.Where(i => i.Aantal > 0).ToList();

            _logger.LogInformation($"Geselecteerd {geselecteerd.Count} items voor bestelling");

            if (!geselecteerd.Any())
            {
                ModelState.AddModelError(string.Empty, "Kies minstens een item.");
                return View(vm);
            }

            var bevestigingVm = new BestellingBevestigingViewModel
            {
                ReservatieId = vm.ReservatieId,
                Items = geselecteerd,
                Totaal = geselecteerd.Sum(i => i.Aantal * i.Prijs)
            };

            TempData["GeselecteerdeItems"] = JsonSerializer.Serialize(geselecteerd);

            return View("Bevestiging", bevestigingVm);
        }

        // POST: Bestelling/Bevestiging
        // Slaat bestelling op en redirect naar succes pagina.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Bevestiging(BestellingBevestigingViewModel vm)
        {
            var json = TempData["GeselecteerdeItems"] as string;
            var items = json != null ? JsonSerializer.Deserialize<List<BestellingMenuItemViewModel>>(json) : new List<BestellingMenuItemViewModel>();

            TempData.Keep("GeselecteerdeItems"); // Bewaar voor mogelijke terugkeer naar menu

            if (items == null || !items.Any())
            {
                return RedirectToAction(nameof(Menu), new { reservatieId = vm.ReservatieId });
            }

            _logger.LogInformation($"Bevestiging: {items.Count} items om op te slaan");
            _logger.LogInformation($"Items: {string.Join(", ", items.Select(i => $"{i.ItemId}:{i.Aantal}"))}");

            var reservatieExists = await _unitOfWork.ReservatiesRepository.GetByIdAsync(vm.ReservatieId) != null;

            try
            {
                await _unitOfWork.BestellingRepository.SaveBestellingenAsync(items, vm.ReservatieId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Fout bij het opslaan van de bestelling.");
                ModelState.AddModelError(string.Empty, "Er is een fout opgetreden bij het opslaan van de bestelling.");
                return RedirectToAction(nameof(Menu), new { reservatieId = vm.ReservatieId });
            }

            TempData["BesteldeItems"] = JsonSerializer.Serialize(items);
            TempData["ReservatieId"] = vm.ReservatieId;

            return RedirectToAction(nameof(Gelukt), new { reservatieId = vm.ReservatieId });
        }

        // GET: Bestelling/Gelukt
        // Toont succes scherm met besteloverzicht.
        [HttpGet]
        public async Task<IActionResult> Gelukt(int reservatieId)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository.GetByIdAsync(reservatieId);
            ViewBag.IsReservatieGekoppeld = reservatie != null;

            var json = TempData["BesteldeItems"] as string;
            var besteldeItems = json != null ? JsonSerializer.Deserialize<List<BestellingMenuItemViewModel>>(json) : null;
            TempData.Keep("BesteldeItems");

            if (besteldeItems != null)
            {
                var vm = new BestellingGeluktViewModel
                {
                    ReservatieId = reservatieId,
                    Bestellingen = besteldeItems.Where(item => item.Aantal > 0).Select(item => new BestellingOverzichtItemViewModel
                    {
                        ProductNaam = item.Naam,
                        Aantal = item.Aantal,
                        Prijs = item.Prijs,
                        Subtotaal = item.Aantal * item.Prijs
                    }).ToList(),
                    Totaal = besteldeItems.Where(item => item.Aantal > 0).Sum(item => item.Aantal * item.Prijs)
                };

                return View(vm);
            }
            else
            {
                var vm = await _unitOfWork.BestellingRepository.GetBestellingOverzichtFromDbAsync(reservatieId);

                return View(vm);
            }
        }
    }
}
