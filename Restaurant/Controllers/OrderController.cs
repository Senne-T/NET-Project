using Microsoft.AspNetCore.SignalR;
using Restaurant.Hubs;
using Restaurant.ViewModels.Bestellingen;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Kok + "," + StaticRollen.Ober)]
    public class OrderController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IHubContext<OrderHub> _hub;

        public OrderController(IUnitOfWork uow, IHubContext<OrderHub> hub)
        {
            _uow = uow;
            _hub = hub;
        }

        // GET: /Order/Kok
        [Authorize(Roles = StaticRollen.Kok)]
        public async Task<IActionResult> Kok()
        {
            var bestellingen = await _uow.BestellingRepository.GetOpenBestellingenAsync();

            var model = bestellingen.Select(b => new OrderListViewModel
            {
                BestellingId = b.Id,
                ProductNaam = b.Product?.Naam ?? "-",
                Aantal = b.Aantal,
                Opmerking = b.Opmerking,
                Status = b.Status?.Naam ?? "Onbekend",
                StatusId = b.StatusId,
                TafelNummers = b.Reservatie?.Tafellijsten != null ? string.Join(", ", b.Reservatie.Tafellijsten.Select(t => t.Tafel?.TafelNummer)) : null
            }).ToList();

            return View("/Views/Kok/Index.cshtml", model);
        }

        // GET: /Order/Ober
        [Authorize(Roles = StaticRollen.Ober)]
        public async Task<IActionResult> Ober()
        {
            // Haal ZOWEL dranken (alle statussen) ALS gerechten die klaar zijn (status 2)
            var drankenBestellingen = await _uow.BestellingRepository.GetOpenDrankenAsync();
            var klareGerechten = await _uow.BestellingRepository.GetKlaarVoorOberAsync();
            
            // Combineer beide lijsten
            var alleBestellingen = drankenBestellingen
                .Concat(klareGerechten)
                .OrderBy(b => b.TijdstipBestelling);

            var model = alleBestellingen.Select(b => new OrderListViewModel
            {
                BestellingId = b.Id,
                ProductNaam = b.Product?.Naam ?? "-",
                Aantal = b.Aantal,
                Opmerking = b.Opmerking,
                Status = b.Status?.Naam ?? "Onbekend",
                StatusId = b.StatusId,
                TafelNummers = b.Reservatie?.Tafellijsten != null ? string.Join(", ", b.Reservatie.Tafellijsten.Select(t => t.Tafel?.TafelNummer)) : null
            }).ToList();

            return View("/Views/Ober/Index.cshtml", model);
        }

        [HttpGet]
        public async Task<IActionResult> GetOpenOrders()
        {
            var bestellingen = await _uow.BestellingRepository.GetOpenBestellingenAsync();
            var model = bestellingen.Select(b => new OrderListViewModel
            {
                BestellingId = b.Id,
                ProductNaam = b.Product?.Naam ?? "-",
                Aantal = b.Aantal,
                Opmerking = b.Opmerking,
                Status = b.Status?.Naam ?? "Onbekend",
                StatusId = b.StatusId,
                TafelNummers = b.Reservatie?.Tafellijsten != null ? string.Join(", ", b.Reservatie.Tafellijsten.Select(t => t.Tafel?.TafelNummer)) : null
            }).ToList();

            return Json(model);
        }

        // Nieuwe endpoint voor Ober refresh
        [HttpGet]
        public async Task<IActionResult> GetOberOrders()
        {
            var drankenBestellingen = await _uow.BestellingRepository.GetOpenDrankenAsync();
            var klareGerechten = await _uow.BestellingRepository.GetKlaarVoorOberAsync();
            
            var alleBestellingen = drankenBestellingen
                .Concat(klareGerechten)
                .OrderBy(b => b.TijdstipBestelling);

            var model = alleBestellingen.Select(b => new OrderListViewModel
            {
                BestellingId = b.Id,
                ProductNaam = b.Product?.Naam ?? "-",
                Aantal = b.Aantal,
                Opmerking = b.Opmerking,
                Status = b.Status?.Naam ?? "Onbekend",
                StatusId = b.StatusId,
                TafelNummers = b.Reservatie?.Tafellijsten != null ? string.Join(", ", b.Reservatie.Tafellijsten.Select(t => t.Tafel?.TafelNummer)) : null
            }).ToList();

            return Json(model);
        }

        [HttpGet]
        public async Task<IActionResult> GetOrdersByStatus(int statusId)
        {
            var bestellingen = await _uow.BestellingRepository.GetByStatusAsync(statusId);
            var model = bestellingen.Select(b => new OrderListViewModel
            {
                BestellingId = b.Id,
                ProductNaam = b.Product?.Naam ?? "-",
                Aantal = b.Aantal,
                Opmerking = b.Opmerking,
                Status = b.Status?.Naam ?? "Onbekend",
                StatusId = b.StatusId,
                TafelNummers = b.Reservatie?.Tafellijsten != null ? string.Join(", ", b.Reservatie.Tafellijsten.Select(t => t.Tafel?.TafelNummer)) : null
            }).ToList();

            return Json(model);
        }

        [HttpPost]
        public async Task<IActionResult> UpdateStatus([FromBody] OrderUpdateViewModel model)
        {
            if (model == null) return BadRequest();

            await _uow.BestellingRepository.UpdateStatusAsync(model.Id, model.StatusId);
            await _uow.SaveChangesAsync();

            await _hub.Clients.All.SendAsync("StatusUpdated", model.Id, model.StatusId);
            await _hub.Clients.All.SendAsync("OrderChanged", model.Id, model.StatusId);

            return Ok();
        }
    }
}
