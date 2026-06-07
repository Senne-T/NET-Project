using System.Security.Claims;
using Restaurant.Services;
using System.Net.Mail;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Klant)]
    public class ReservatieController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<ReservatieController> _logger;
        private readonly MailService _mailService;

        public ReservatieController(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            ILogger<ReservatieController> logger,
            MailService mailService)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
            _mailService = mailService;
        }

        private async Task<int> GetIntParameterAsync(string naam)
        {
            var waarde = await _unitOfWork.ParametersRepository.GetValueByNameAsync(naam);
            return int.TryParse(waarde, out int result) ? result : 0;
        }

        private async Task<string> GetStringParameterAsync(string naam)
        {
            var waarde = await _unitOfWork.ParametersRepository.GetValueByNameAsync(naam);
            return waarde ?? "";
        }

        private async Task PopulateParametersAsync(ReservatieViewModel vm)
        {
            vm.RestaurantNaam = await GetStringParameterAsync("RestaurantNaam");
            vm.RestaurantTelefoon = await GetStringParameterAsync("RestaurantTelefoon");
            vm.RestaurantEmail = await GetStringParameterAsync("RestaurantEmail");
            vm.MaxDagenVooruitReserveren = await GetIntParameterAsync("MaxDagenVooruitReserveren");
            vm.AnnulatieTermijn = await GetIntParameterAsync("AnnulatieTermijn");
            vm.MaxPersonenPerReservatie = await GetIntParameterAsync("MaxPersonenPerReservatie");
            vm.MinPersonenPerReservatie = await GetIntParameterAsync("MinPersonenPerReservatie");
        }

        // GET: Reservatie
        public async Task<IActionResult> Index()
        {
            var tijdslots = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();

            var model = new ReservatieViewModel
            {
                Datum = DateTime.Today.AddDays(1),
                AantalPersonen = 2,
                TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslots.OrderBy(t => t.Id).ToList()
                )
            };

            await PopulateParametersAsync(model);

            return View(model);
        }

        // POST: Reservatie/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ReservatieViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var tijdslotsInvalid = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslotsInvalid.OrderBy(t => t.Id).ToList()
                );
                await PopulateParametersAsync(model);
                return View("Index", model);
            }

            var beschikbaarheid = await _unitOfWork.ReservatiesRepository
                .ControleerBeschikbaarheidAsync(model.Datum, model.TijdSlotId, model.AantalPersonen);

            if (!beschikbaarheid.IsBeschikbaar)
            {
                ModelState.AddModelError("", beschikbaarheid.Bericht);

                var tijdslotsBeschikbaar = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslotsBeschikbaar.OrderBy(t => t.Id).ToList()
                );
                await PopulateParametersAsync(model);
                model.IsBeschikbaar = false;
                model.BeschikbaarheidsBericht = beschikbaarheid.Bericht;

                return View("Index", model);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                ModelState.AddModelError("", "U moet ingelogd zijn om een reservatie te maken.");
                var tijdslotsNoUser = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslotsNoUser.OrderBy(t => t.Id).ToList()
                );
                await PopulateParametersAsync(model);
                return View("Index", model);
            }

            var success = await _unitOfWork.ReservatiesRepository.MaakReservatieAsync(userId, model);

            if (success)
            {
                try
                {
                    // Claims ophalen
                    var voornaam = User.FindFirstValue(ClaimTypes.GivenName) ?? "";
                    var achternaam = User.FindFirstValue(ClaimTypes.Surname) ?? "";
                    var email = User.FindFirstValue(ClaimTypes.Email) ?? "";

                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        //Tijdslotnaam voor placeholder
                        var tijdslots = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                        var tijdslot = tijdslots.FirstOrDefault(t => t.Id == model.TijdSlotId);

                        var placeholder = new Dictionary<string, string>
                        {
                            ["VOORNAAM"] = voornaam,
                            ["ACHTERNAAM"] = achternaam,
                            ["DATUM"] = model.Datum.ToString("dd-MM-yyyy"),
                            ["TIJD"] = tijdslot?.Naam ?? "",
                            ["AANTAL"] = model.AantalPersonen.ToString(),
                            ["TAFEL"] = "Wordt bij aankomst toegewezen.",
                            ["NAAM"] = model.RestaurantNaam ?? await GetStringParameterAsync("RestaurantNaam")
                            
                        };

                        var template = await _unitOfWork.MailRepository.GetMailByNameAsync("BevestigingMail");

                        if (template == null)
                        {
                            _logger.LogError("E-mail template 'BevestigingMail' niet gevonden.");
                        }
                        else
                        {
                            await _mailService.SendMailAsync(
                                email,
                                template.Onderwerp ?? "Bevestiging van uw reservatie",
                                template.Body ?? "",
                                placeholder
                            );
                        }
                    }
                    else
                    {
                        _logger.LogWarning("BevestigingMail niet verzonden: email claim ontbreekt");
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Fout bij het verzenden van de bevestigingMail.");
                }

                TempData["SuccessMessage"] = "Uw reservatie is succesvol aangemaakt!";
                return RedirectToAction("Bevestiging");
            }
            else
            {
                ModelState.AddModelError("", "Er is een fout opgetreden bij het maken van de reservatie. Probeer het opnieuw.");

                var tijdslotsError = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslotsError.OrderBy(t => t.Id).ToList()
                );
                await PopulateParametersAsync(model);
                return View("Index", model);
            }
        }

        // GET: Reservatie/GetTijdslotBeschikbaarheid
        [HttpGet]
        public async Task<IActionResult> GetTijdslotBeschikbaarheid(DateTime datum, int aantalPersonen)
        {
            if (datum < DateTime.Today)
            {
                return Json(new { error = "Datum mag niet in het verleden liggen" });
            }

            var tijdslots = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
            var result = new List<TijdslotBeschikbaarheidViewModel>();

            foreach (var tijdslot in tijdslots.OrderBy(t => t.Id))
            {
                var beschikbaarheid = await _unitOfWork.ReservatiesRepository
                    .ControleerBeschikbaarheidAsync(datum, tijdslot.Id, aantalPersonen);

                int aantalBeschikbareGroepen = await _unitOfWork.ReservatiesRepository
                    .BerekenAantalBeschikbareGroepenAsync(datum, tijdslot.Id, aantalPersonen);

                var viewModel = _mapper.Map<TijdslotBeschikbaarheidViewModel>(tijdslot);
                viewModel.IsBeschikbaar = beschikbaarheid.IsBeschikbaar;
                viewModel.BeschikbarePlaatsen = aantalBeschikbareGroepen > 0 ? aantalPersonen : 0;

                result.Add(viewModel);
            }

            return Json(result);
        }

        // GET: Reservatie/Bevestiging
        public IActionResult Bevestiging()
        {
            return View();
        }

        // GET: Reservatie/MyReservations
        public async Task<IActionResult> MyReservations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Gebruiker");
            }

            var reservaties = await _unitOfWork.ReservatiesRepository.GetReservatiesByUserIdAsync(userId);
            _logger.LogInformation($"Gevonden {reservaties.Count} reservaties voor user {userId}");

            foreach (var r in reservaties)
            {
                _logger.LogInformation($"Reservatie ID: {r.Id}, Datum: {r.Datum}, TijdSlot: {r.Tijdslot?.Naam}, Aantal: {r.AantalPersonen}");
            }

            var list = _mapper.Map<List<ReservatieListViewModel>>(reservaties);
            _logger.LogInformation($"Mapped naar {list.Count} view models");

            var annulatieTermijnStr = await _unitOfWork.ParametersRepository.GetValueByNameAsync("AnnulatieTermijn");
            int annulatieTermijn = int.TryParse(annulatieTermijnStr, out var temp) ? temp : 24;

            foreach (var item in list)
            {
                item.CanCancel = item.Datum.HasValue &&
                                 item.Datum.Value.Date > DateTime.Today.AddHours(annulatieTermijn);
            }

            // Create the view model with filtered reservations
            var model = new MyReservationsViewModel
            {
                UpcomingReservations = list.Where(r => r.Datum.HasValue && r.Datum.Value.Date >= DateTime.Today).ToList(),
                PastReservations = list.Where(r => r.Datum.HasValue && r.Datum.Value.Date < DateTime.Today).ToList()
            };

            await PopulateParametersAsync(new ReservatieViewModel()); // optioneel, voor gedeelde params

            return View(model);
        }

        // GET: Reservatie/Edit/id
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Gebruiker");
            }

            var reservatie = await _unitOfWork.ReservatiesRepository.GetByIdAsync(id);
            if (reservatie == null || reservatie.KlantId != userId)
            {
                return RedirectToAction("Index", "Home");
            }

            var model = _mapper.Map<ReservatieViewModel>(reservatie);

            var tijdslots = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
            model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                tijdslots.OrderBy(t => t.Id).ToList()
            );

            await PopulateParametersAsync(model);

            return View(model);
        }

        // POST: Reservatie/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ReservatieViewModel model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Gebruiker");
            }

            var reservatie = await _unitOfWork.ReservatiesRepository.GetByIdAsync(id);
            if (reservatie == null || reservatie.KlantId != userId)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                var tijdslotsInvalid = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslotsInvalid.OrderBy(t => t.Id).ToList()
                );
                await PopulateParametersAsync(model);
                return View(model);
            }

            var success = await _unitOfWork.ReservatiesRepository.WijzigReservatieAsync(id, userId, model);

            if (success)
            {
                TempData["SuccessMessage"] = "Uw reservatie is succesvol gewijzigd!";
                return RedirectToAction("MyReservations");
            }
            else
            {
                ModelState.AddModelError("", "Er is een fout opgetreden bij het wijzigen van de reservatie. Controleer of de nieuwe gegevens beschikbaar zijn.");

                var tijdslotsError = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
                model.TijdSlotOpties = _mapper.Map<List<SelectListItem>>(
                    tijdslotsError.OrderBy(t => t.Id).ToList()
                );
                await PopulateParametersAsync(model);
                return View(model);
            }
        }

        // POST: Reservatie/Cancel/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return RedirectToAction("Login", "Gebruiker");
            }

            var reservatie = await _unitOfWork.ReservatiesRepository.GetByIdAsync(id);
            if (reservatie == null || reservatie.KlantId != userId)
            {
                return RedirectToAction("Index", "Home");
            }

            var success = await _unitOfWork.ReservatiesRepository.AnnuleerReservatieAsync(id, userId);

            if (success)
            {
                TempData["SuccessMessage"] = "Uw reservatie is succesvol geannuleerd.";
            }
            else
            {
                TempData["ErrorMessage"] = "Annulatie niet mogelijk. Controleer of het nog binnen de termijn valt.";
            }

            return RedirectToAction("MyReservations");
        }

        // POST: Reservatie/VerstuurEnqueteEmail/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerstuurEnqueteEmail(int id)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository.GetReservatieWithUserAsync(id);
            if (reservatie == null || reservatie.CustomUser == null)
            {
                TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                return RedirectToAction("Index");
            }

            var emailVerzendingActief = await _unitOfWork.ParametersRepository.GetValueByNameAsync("EmailVerzendingActief");
            if (emailVerzendingActief != "1")
            {
                TempData["ErrorMessage"] = "E-mail verzending is uitgeschakeld.";
                return RedirectToAction("Index");
            }

            var mailTemplate = await _unitOfWork.MailRepository.GetMailByNameAsync("EvaluatieMail");
            if (mailTemplate == null)
            {
                TempData["ErrorMessage"] = "Evaluatie e-mailsjabloon niet gevonden.";
                return RedirectToAction("Index");
            }

            var surveyUrl = Url.Action("Index", "Enquete", new { reservatieId = id }, Request.Scheme, Request.Host.ToString());

            var replacements = new Dictionary<string, string>
            {
                { "VOORNAAM", reservatie.CustomUser.Voornaam ?? "" },
                { "ACHTERNAAM", reservatie.CustomUser.Achternaam ?? "" },
                { "DATUM", reservatie.Datum?.ToString("dd/MM/yyyy") ?? "" },
                { "TIJD", reservatie.Tijdslot?.Naam ?? "" },
                { "AANTAL", reservatie.AantalPersonen.ToString() },
                { "SURVEYLINK", $"<a href=\"{surveyUrl}\">Klik hier om de enquête in te vullen</a>" }
            };

            //template e-mail versturen
            await _mailService.SendMailAsync(
                reservatie.CustomUser.Email!,
                mailTemplate.Onderwerp ?? "Hoe was uw bezoek?",
                mailTemplate.Body ?? "",
                replacements
            );

            if (Request.Headers.ContainsKey("X-Requested-With") && Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = $"Evaluatie e-mail verzonden naar {reservatie.CustomUser.Email}" });
            }

            TempData["SuccessMessage"] = "Evaluatie e-mail verzonden.";
            return RedirectToAction("Index");
        }
    }
}