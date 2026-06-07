using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Models;
using Restaurant.ViewModels;
using Restaurant.ViewModels.Tafel;
using Restaurant.ViewModels.Reservatie;
using Restaurant.Data.UnitOfWork;
using Restaurant.Services;
using AutoMapper;

namespace Restaurant.Controllers
{
    public class TafelController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly MailService _mailService;
        private readonly ILogger<TafelController> _logger;
        

        public TafelController(IUnitOfWork unitOfWork, IMapper mapper, MailService mailService, ILogger<TafelController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _mailService = mailService;
            _logger = logger;
        }

        // Overzicht tafels voor datum en tijdslot
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> Index(DateTime? datum)
        {
            var gekozenDatum = (datum ?? DateTime.Today).Date;

            // 1. Alle actieve tijdsloten ophalen via dedicated repository
            var tijdsloten = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();

            // 2. Alle reservaties voor deze datum ophalen (alle tijdsloten) - NIET betaald
            List<ReservatieOverzichtViewModel> alleReservatiesVandaag;

            if (tijdsloten.Any())
            {
                var reservaties = await _unitOfWork.ReservatiesRepository
                    .GetReservatiesVoorDatumAsync(gekozenDatum);

                // Map naar ViewModel via AutoMapper
                alleReservatiesVandaag = _mapper.Map<List<ReservatieOverzichtViewModel>>(reservaties);
            }
            else
            {
                // Geen tijdsloten → geen reservaties
                alleReservatiesVandaag = new List<ReservatieOverzichtViewModel>();
            }

            // 3. Haal betaalde reservaties op voor onderaan
            var betaaldeReservaties = await _unitOfWork.ReservatiesRepository
                .GetBetaaldeReservatiesVoorDatumAsync(gekozenDatum);
            var betaaldeReservatiesViewModel = _mapper.Map<List<ReservatieOverzichtViewModel>>(betaaldeReservaties);

            // 4. Haal alle actieve tafels op voor overzicht
            var alleTafels = await _unitOfWork.TafelRepository.GetActiveTafelsAsync();

            // 5. datum/tijdsloten doorgeven aan view
            ViewData["GekozenDatum"] = gekozenDatum;
            ViewData["Tijdsloten"] = tijdsloten;
            ViewData["Reservaties"] = alleReservatiesVandaag;
            ViewData["BetaaldeReservaties"] = betaaldeReservatiesViewModel;
            ViewData["AlleTafels"] = alleTafels;

            return View();
        }

        // GET: Overzicht van reservaties voor betaling
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> Betalingen(DateTime? datum, int? tijdslotId)
        {
            var gekozenDatum = (datum ?? DateTime.Today).Date;
            
            var tijdsloten = await _unitOfWork.TijdslotRepository.GetActiveTijdslotsAsync();
            
            if (!tijdsloten.Any())
            {
                ViewData["Tijdsloten"] = new List<Tijdslot>();
                ViewData["GekozenDatum"] = gekozenDatum;
                ViewData["GekozenTijdslotId"] = 0;
                return View(new List<ReservatieMetFactuurViewModel>());
            }

            if (!tijdslotId.HasValue)
            {
                tijdslotId = tijdsloten.First().Id;
            }

            var reservaties = await _unitOfWork.ReservatiesRepository
                .GetReservatiesMetBestellingenVoorDatumEnTijdslotAsync(gekozenDatum, tijdslotId.Value);

            var viewModels = reservaties.Select(reservatie =>
            {
                var viewModel = _mapper.Map<ReservatieMetFactuurViewModel>(reservatie);
                
                // Calculate total amount (not mapped by AutoMapper)
                var bestellingen = reservatie.Bestellingen?.Where(b => b.StatusId != 4).ToList() ?? new List<Bestelling>();
                
                if (bestellingen.Any())
                {
                    viewModel.TotaalBedrag = bestellingen.Sum(b => 
                    {
                        var prijs = b.Product?.PrijsProducten?
                            .OrderByDescending(p => p.DatumVanaf)
                            .FirstOrDefault()?.Prijs ?? 0m;
                        return b.Aantal * prijs;
                    });
                }
                else
                {
                    viewModel.TotaalBedrag = 0m;
                }
                
                _logger.LogInformation($"Reservatie {reservatie.Id}: {bestellingen.Count} bestellingen, Totaal: €{viewModel.TotaalBedrag:F2}");
                
                return viewModel;
            }).OrderBy(r => r.Tafels).ToList();

            ViewData["GekozenDatum"] = gekozenDatum;
            ViewData["GekozenTijdslotId"] = tijdslotId ?? 0;
            ViewData["Tijdsloten"] = tijdsloten;

            return View(viewModels);
        }

        // GET: Factuur voor een reservatie
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> Factuur(int id)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository
                .GetReservatieMetBestellingenAsync(id);

            if (reservatie == null)
            {
                TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                return RedirectToAction(nameof(Betalingen));
            }

            var bestellingen = reservatie.Bestellingen
                .Where(b => b.StatusId != 4) // Exclude cancelled orders
                .ToList();

            if (!bestellingen.Any())
            {
                TempData["ErrorMessage"] = "Er zijn geen geleverde bestellingen voor deze reservatie.";
                return RedirectToAction(nameof(Betalingen));
            }

            var factuur = await BouwFactuurViewModelAsync(reservatie, bestellingen);

            return View(factuur);
        }

        // GET: Betaling verwerken
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> Afrekenen(int id)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository
                .GetReservatieMetBestellingenAsync(id);

            if (reservatie == null)
            {
                TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                return RedirectToAction(nameof(Betalingen));
            }

            if (reservatie.Bestaald)
            {
                TempData["ErrorMessage"] = "Deze reservatie is al betaald.";
                return RedirectToAction(nameof(Factuur), new { id });
            }

            var bestellingen = reservatie.Bestellingen
                .Where(b => b.StatusId != 4)
                .ToList();

            if (!bestellingen.Any())
            {
                TempData["ErrorMessage"] = "Er zijn geen geleverde bestellingen voor deze reservatie.";
                return RedirectToAction(nameof(Betalingen));
            }

            var afrekeningViewModel = await BouwAfrekeningViewModelAsync(reservatie, bestellingen);
            
            return View(afrekeningViewModel);
        }

        // POST: Annuleer bestelling
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> AnnuleerBestelling(int bestellingId, int reservatieId)
        {
            try
            {
                var bestelling = await _unitOfWork.BestellingRepository.GetByIdAsync(bestellingId);
                
                if (bestelling == null || bestelling.ReservatieId != reservatieId)
                {
                    return Json(new { success = false, message = "Bestelling niet gevonden." });
                }

                // Annuleer alleen deze specifieke bestelling
                bestelling.StatusId = 4; // Geannuleerd
                _unitOfWork.BestellingRepository.Update(bestelling);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Bestelling {bestellingId} (Product: {bestelling.ProductId}, Aantal: {bestelling.Aantal}) geannuleerd voor reservatie {reservatieId}");

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij annuleren bestelling {bestellingId}");
                return Json(new { success = false, message = "Er is een fout opgetreden bij het annuleren." });
            }
        }

        // POST: Wijzig aantal van gegroepeerde items
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> WijzigAantal(int reservatieId, string productNaam, int nieuwAantal)
        {
            try
            {
                if (nieuwAantal < 0)
                {
                    return Json(new { success = false, message = "Aantal moet positief zijn." });
                }

                // Haal alle bestellingen van dit product op
                var reservatie = await _unitOfWork.ReservatiesRepository.GetReservatieMetBestellingenAsync(reservatieId);
                
                if (reservatie == null)
                {
                    return Json(new { success = false, message = "Reservatie niet gevonden." });
                }

                var bestellingen = reservatie.Bestellingen
                    .Where(b => b.Product.Naam == productNaam && b.StatusId != 4)
                    .OrderBy(b => b.Id)
                    .ToList();

                if (!bestellingen.Any())
                {
                    return Json(new { success = false, message = "Geen bestellingen gevonden voor dit product." });
                }

                // Bereken huidige totaal aantal
                int huidigAantal = bestellingen.Sum(b => b.Aantal);
                int verschil = nieuwAantal - huidigAantal;

                if (verschil == 0)
                {
                    // Geen wijziging
                    return Json(new { success = true, message = "Geen wijziging" });
                }
                else if (verschil < 0)
                {
                    // Verminder aantal: annuleer bestellingen van achter naar voor
                    int teVerminderen = Math.Abs(verschil);
                    
                    foreach (var bestelling in bestellingen.OrderByDescending(b => b.Id))
                    {
                        if (teVerminderen <= 0) break;

                        if (bestelling.Aantal <= teVerminderen)
                        {
                            // Annuleer hele bestelling
                            bestelling.StatusId = 4;
                            teVerminderen -= bestelling.Aantal;
                            _unitOfWork.BestellingRepository.Update(bestelling);
                        }
                        else
                        {
                            // Verminder aantal van deze bestelling
                            bestelling.Aantal -= teVerminderen;
                            teVerminderen = 0;
                            _unitOfWork.BestellingRepository.Update(bestelling);
                        }
                    }
                }
                else
                {
                    // Verhoog aantal: voeg toe aan eerste niet-geannuleerde bestelling
                    var eersteBestelling = bestellingen.First();
                    eersteBestelling.Aantal += verschil;
                    _unitOfWork.BestellingRepository.Update(eersteBestelling);
                }

                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Aantal van {productNaam} gewijzigd van {huidigAantal} naar {nieuwAantal} voor reservatie {reservatieId}");

                // Bereken nieuwe prijs
                var prijs = bestellingen.First().Product.PrijsProducten
                    .OrderByDescending(p => p.DatumVanaf)
                    .First().Prijs;
                var nieuwSubtotaal = nieuwAantal * prijs;

                return Json(new 
                { 
                    success = true, 
                    nieuwAantal = nieuwAantal,
                    nieuwSubtotaal = nieuwSubtotaal,
                    prijs = prijs
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij wijzigen aantal voor reservatie {reservatieId}");
                return Json(new { success = false, message = "Er is een fout opgetreden." });
            }
        }

        // POST: Verwerk betaling
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> Afrekenen(int reservatieId, string betaalmethode)
        {
            if (string.IsNullOrEmpty(betaalmethode))
            {
                TempData["ErrorMessage"] = "Kies een betaalmethode.";
                return RedirectToAction(nameof(Afrekenen), new { id = reservatieId });
            }

            try
            {
                var reservatie = await _unitOfWork.ReservatiesRepository
                    .GetReservatieMetBestellingenAsync(reservatieId);

                if (reservatie == null)
                {
                    TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                    return RedirectToAction(nameof(Betalingen));
                }

                if (reservatie.Bestaald)
                {
                    TempData["ErrorMessage"] = "Deze reservatie is al betaald.";
                    return RedirectToAction(nameof(Factuur), new { id = reservatieId });
                }

                // Simulate payment processing for Payconiq
                if (betaalmethode.ToLower() == "payconiq")
                {
                    // In a real application, this would integrate with Payconiq API
                    await Task.Delay(1000); // Simulate API call
                }

                // Mark reservation as paid
                reservatie.Bestaald = true;
                _unitOfWork.ReservatiesRepository.Update(reservatie);
                await _unitOfWork.SaveChangesAsync();

                _logger.LogInformation($"Betaling verwerkt voor reservatie {reservatieId} via {betaalmethode}");

                // Send thank you email with invoice
                try
                {
                    await VerstuurFactuurEmailAsync(reservatie);
                }
                catch (Exception emailEx)
                {
                    _logger.LogError(emailEx, $"Fout bij verzenden email voor reservatie {reservatieId}: {emailEx.Message}");
                    TempData["WarningMessage"] = $"Betaling succesvol verwerkt, maar email kon niet verzonden worden: {emailEx.Message}";
                    return RedirectToAction(nameof(BetalingBevestiging), new { id = reservatieId });
                }

                TempData["SuccessMessage"] = "Betaling succesvol verwerkt en email verzonden.";
                return RedirectToAction(nameof(BetalingBevestiging), new { id = reservatieId });

            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij verwerken betaling voor reservatie {reservatieId}");
                TempData["ErrorMessage"] = $"Er is een fout opgetreden: {ex.Message}";
                return RedirectToAction(nameof(Afrekenen), new { id = reservatieId });
            }
        }

        // GET: Betalingsbevestiging
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> BetalingBevestiging(int id)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository
                .GetReservatieMetBestellingenAsync(id);

            if (reservatie == null)
            {
                TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                return RedirectToAction(nameof(Betalingen));
            }

            var bestellingen = reservatie.Bestellingen
                .Where(b => b.StatusId != 4)
                .ToList();

            var factuur = await BouwFactuurViewModelAsync(reservatie, bestellingen);

            return View(factuur);
        }

        // Helper method to build FactuurViewModel
        private async Task<FactuurViewModel> BouwFactuurViewModelAsync(Reservatie reservatie, List<Bestelling> bestellingen)
        {
            // Haal BTW percentage op
            var btwPercentageParam = await _unitOfWork.ParametersRepository.FindByNameAsync("BTWPercentage");
            var btwPercentage = decimal.TryParse(btwPercentageParam?.Waarde ?? "21", out var btw) ? btw / 100 : 0.21m;

            // Map basis eigenschappen via AutoMapper
            var factuur = _mapper.Map<FactuurViewModel>(reservatie);

            // Map bestellingen naar factuurregels
            var factuurRegels = _mapper.Map<List<FactuurRegelViewModel>>(bestellingen);

            // Groepeer dezelfde producten en combineer de aantallen
            factuur.Regels = factuurRegels
                .GroupBy(r => r.ProductNaam)
                .Select(g => new FactuurRegelViewModel
                {
                    BestellingId = g.First().BestellingId,
                    ProductNaam = g.Key,
                    Aantal = g.Sum(r => r.Aantal),
                    Prijs = g.First().Prijs,
                    Geannuleerd = g.Any(r => r.Geannuleerd)
                })
                .OrderBy(r => r.ProductNaam)
                .ToList();

            // Prijzen zijn inclusief BTW - we moeten BTW eruit halen
            var totaalInclusiefBtw = factuur.Regels.Sum(r => r.Subtotaal);
            var totaalExclusiefBtw = totaalInclusiefBtw / (1 + btwPercentage);
            var btwBedrag = totaalInclusiefBtw - totaalExclusiefBtw;

            factuur.Subtotaal = totaalExclusiefBtw;
            factuur.BTW = btwBedrag;
            factuur.Totaal = totaalInclusiefBtw;

            return factuur;
        }

        // Helper method to build AfrekeningViewModel
        private async Task<AfrekeningViewModel> BouwAfrekeningViewModelAsync(Reservatie reservatie, List<Bestelling> bestellingen)
        {
            // Haal BTW percentage op
            var btwPercentageParam = await _unitOfWork.ParametersRepository.FindByNameAsync("BTWPercentage");
            var btwPercentage = decimal.TryParse(btwPercentageParam?.Waarde ?? "21", out var btw) ? btw / 100 : 0.21m;

            // Map basis eigenschappen via AutoMapper
            var afrekening = _mapper.Map<AfrekeningViewModel>(reservatie);

            // Map bestellingen naar factuurregels
            var factuurRegels = _mapper.Map<List<FactuurRegelViewModel>>(bestellingen);

            // Groepeer dezelfde producten en bewaar ALLE BestellingIds voor annulatie functionaliteit
            afrekening.Regels = factuurRegels
                .GroupBy(r => r.ProductNaam)
                .Select(g => new FactuurRegelViewModel
                {
                    BestellingId = g.First().BestellingId,
                    ProductNaam = g.Key,
                    Aantal = g.Sum(r => r.Aantal),
                    Prijs = g.First().Prijs,
                    Geannuleerd = g.Any(r => r.Geannuleerd),
                    AlleBestellingIds = g.Select(r => r.BestellingId).ToList()
                })
                .OrderBy(r => r.ProductNaam)
                .ToList();

            // Prijzen zijn inclusief BTW - we moeten BTW eruit halen
            var totaalInclusiefBtw = afrekening.Regels.Sum(r => r.Subtotaal);
            var totaalExclusiefBtw = totaalInclusiefBtw / (1 + btwPercentage);
            var btwBedrag = totaalExclusiefBtw - totaalInclusiefBtw;

            afrekening.Subtotaal = totaalExclusiefBtw;
            afrekening.BTW = btwBedrag;
            afrekening.Totaal = totaalInclusiefBtw;

            return afrekening;
        }

        private async Task VerstuurFactuurEmailAsync(Reservatie reservatie)
        {
            try
            {
                _logger.LogInformation($"Start verzenden factuur email voor reservatie {reservatie.Id}");
                
                var mailTemplate = await _unitOfWork.MailRepository.GetMailByNameAsync("BedankMail");
                _logger.LogInformation($"BedankMail template opgehaald: {mailTemplate?.Naam}");
                
                if (mailTemplate == null)
                {
                    _logger.LogError("BedankMail template niet gevonden in database");
                    throw new InvalidOperationException("BedankMail template niet gevonden in database. Controleer of het template bestaat in de Mail tabel.");
                }
                
                if (string.IsNullOrEmpty(reservatie.CustomUser?.Email))
                {
                    _logger.LogError($"Klant email is leeg voor reservatie {reservatie.Id}");
                    throw new InvalidOperationException($"Klant email is leeg voor reservatie {reservatie.Id}");
                }
                
                var bestellingen = reservatie.Bestellingen.Where(b => b.StatusId != 4).ToList();
                _logger.LogInformation($"Gevonden {bestellingen.Count} niet-geannuleerde bestellingen");
                
                var btwPercentageParam = await _unitOfWork.ParametersRepository.FindByNameAsync("BTWPercentage");
                var btwPercentage = decimal.TryParse(btwPercentageParam?.Waarde ?? "21", out var btw) ? btw / 100 : 0.21m;
                
                var factuurRegels = _mapper.Map<List<FactuurRegelViewModel>>(bestellingen);
                
                // Groepeer dezelfde producten en combineer de aantallen voor email
                var gegroepeerdeRegels = factuurRegels
                    .GroupBy(r => r.ProductNaam)
                    .Select(g => new
                    {
                        ProductNaam = g.Key,
                        Aantal = g.Sum(r => r.Aantal),
                        Prijs = g.First().Prijs,
                        Subtotaal = g.Sum(r => r.Subtotaal)
                    })
                    .OrderBy(r => r.ProductNaam)
                    .ToList();
                
                // Prijzen zijn inclusief BTW - we moeten BTW eruit halen
                var totaalInclusiefBtw = gegroepeerdeRegels.Sum(r => r.Subtotaal);
                var totaalExclusiefBtw = totaalInclusiefBtw / (1 + btwPercentage);
                var btwBedrag = totaalInclusiefBtw - totaalExclusiefBtw;

                var factuurDetails = string.Join("\n", gegroepeerdeRegels.Select(r => 
                    $"{r.ProductNaam} x{r.Aantal} - €{r.Subtotaal:F2}"));

                var restaurantNaamParam = await _unitOfWork.ParametersRepository.FindByNameAsync("RestaurantNaam");
                var restaurantNaam = restaurantNaamParam?.Waarde ?? "Restaurant";

                var surveyLink = $"{Request.Scheme}://{Request.Host}/Enquete/Index?reservatieId={reservatie.Id}";
                _logger.LogInformation($"Survey link: {surveyLink}");

                var replacements = new Dictionary<string, string>
                {
                    { "VOORNAAM", reservatie.CustomUser.Voornaam ?? "" },
                    { "ACHTERNAAM", reservatie.CustomUser.Achternaam ?? "" },
                    { "DATUM", reservatie.Datum?.ToString("dd/MM/yyyy") ?? "" },
                    { "TIJD", reservatie.Tijdslot.Naam ?? "" },
                    { "AANTAL", reservatie.AantalPersonen.ToString() },
                    { "TAFEL", string.Join(", ", reservatie.Tafellijsten.Select(tl => tl.Tafel.TafelNummer)) },
                    { "FACTUUR", factuurDetails },
                    { "SUBTOTAAL", $"€{totaalExclusiefBtw:F2}" },
                    { "BTW", $"€{btwBedrag:F2}" },
                    { "TOTAAL", $"€{totaalInclusiefBtw:F2}" },
                    { "SURVEYLINK", surveyLink },
                    { "RESTAURANTNAAM", restaurantNaam }
                };

                _logger.LogInformation($"Versturen email naar: {reservatie.CustomUser.Email}");
                _logger.LogInformation($"Email onderwerp: {mailTemplate.Onderwerp}");

                await _mailService.SendMailAsync(
                    reservatie.CustomUser.Email,
                    mailTemplate.Onderwerp,
                    mailTemplate.Body,
                    replacements
                );

                _logger.LogInformation($"Factuur email succesvol verzonden naar {reservatie.CustomUser.Email} voor reservatie {reservatie.Id}");
            }
            catch (KeyNotFoundException ex)
            {
                _logger.LogError(ex, $"BedankMail template niet gevonden in database voor reservatie {reservatie.Id}");
                throw new InvalidOperationException("BedankMail email template ontbreekt. Voeg dit toe via /Mail/Create met Naam='BedankMail'", ex);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, $"Configuratie probleem bij versturen factuur email voor reservatie {reservatie.Id}");
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Onverwachte fout bij versturen factuur email voor reservatie {reservatie.Id}: {ex.Message}");
                throw new InvalidOperationException($"Fout bij verzenden factuur email: {ex.Message}. Controleer SMTP instellingen en email templates.", ex);
            }
        }

        // GET: Toon tafeltoewijzing scherm
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> TafelToewijzen(int reservatieId, DateTime datum)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository.GetReservatieWithUserAsync(reservatieId);
            
            if (reservatie == null)
            {
                TempData["ErrorMessage"] = "Reservatie niet gevonden.";

                // Refresh de lijst met reservaties voor de geselecteerde datum
                var geselecteerdeDatum = datum.ToString("yyyy-MM-dd");
                return RedirectToAction(nameof(Index), new { datum = geselecteerdeDatum });
            }

            if (reservatie.IsAanwezig)
            {
                TempData["WarningMessage"] = "Deze klant is al aanwezig en toegewezen aan een tafel.";
                return RedirectToAction(nameof(Index), new { datum });
            }

            // Haal alle actieve tafels op
            var alleTafels = await _unitOfWork.TafelRepository.GetActiveTafelsAsync();
            
            // Haal bezette tafels op voor dit tijdslot
            var reservatiesVoorTijdslot = await _unitOfWork.ReservatiesRepository
                .GetReservatiesVoorDatumEnTijdslotAsync(datum, reservatie.TijdSlotId);
            
            var bezeteTafelIds = reservatiesVoorTijdslot
                .Where(r => r.IsAanwezig && r.Id != reservatieId)
                .SelectMany(r => r.Tafellijsten.Select(tl => tl.TafelId))
                .Distinct()
                .ToList();

            var huidigeGereserveerdeTafelIds = reservatie.Tafellijsten.Select(tl => tl.TafelId).ToList();

            var viewModel = new TafelToewijzingViewModel
            {
                ReservatieId = reservatie.Id,
                KlantNaam = $"{reservatie.CustomUser.Voornaam} {reservatie.CustomUser.Achternaam}",
                AantalPersonen = reservatie.AantalPersonen,
                Datum = datum,
                TijdslotNaam = reservatie.Tijdslot.Naam,
                GereserveerdeTafels = string.Join(", ", reservatie.Tafellijsten.Select(tl => tl.Tafel.TafelNummer)),
                BeschikbareTafels = alleTafels.Select(t => new TafelOptieViewModel
                {
                    TafelId = t.Id,
                    TafelNummer = t.TafelNummer,
                    AantalPersonen = t.AantalPersonen,
                    MinAantalPersonen = t.MinAantalPersonen,
                    IsBezet = bezeteTafelIds.Contains(t.Id),
                    IsGereserveerd = huidigeGereserveerdeTafelIds.Contains(t.Id)
                }).OrderBy(t => t.TafelNummer).ToList()
            };

            return View(viewModel);
        }

        // POST: Wijs tafels toe en markeer als aanwezig
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> TafelToewijzen(int reservatieId, DateTime datum, List<int> geselecteerdeTafels)
        {
            try
            {
                if (geselecteerdeTafels == null || !geselecteerdeTafels.Any())
                {
                    TempData["ErrorMessage"] = "Selecteer minimaal één tafel.";
                    return RedirectToAction(nameof(TafelToewijzen), new { reservatieId, datum });
                }

                var reservatie = await _unitOfWork.ReservatiesRepository.GetReservatieWithUserAsync(reservatieId);
                
                if (reservatie == null)
                {
                    TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                    return RedirectToAction(nameof(Index), new { datum });
                }

                // Haal de geselecteerde tafels op om capaciteit te controleren
                var alleTafels = await _unitOfWork.TafelRepository.GetActiveTafelsAsync();
                var geselecteerdeTafelObjecten = alleTafels.Where(t => geselecteerdeTafels.Contains(t.Id)).ToList();

                // Controleer of alle geselecteerde tafels bestaan
                if (geselecteerdeTafelObjecten.Count != geselecteerdeTafels.Count)
                {
                    TempData["ErrorMessage"] = "Een of meerdere geselecteerde tafels zijn niet geldig.";
                    return RedirectToAction(nameof(TafelToewijzen), new { reservatieId, datum });
                }

                // Bereken totale capaciteit van geselecteerde tafels
                int totaleCapaciteit = geselecteerdeTafelObjecten.Sum(t => t.AantalPersonen);
                int minimaleCapaciteit = geselecteerdeTafelObjecten.Sum(t => t.MinAantalPersonen);

                // Valideer of de capaciteit geschikt is
                if (reservatie.AantalPersonen < minimaleCapaciteit)
                {
                    TempData["ErrorMessage"] = $"De geselecteerde tafel(s) hebben een minimale capaciteit van {minimaleCapaciteit} personen, maar de reservatie is voor {reservatie.AantalPersonen} personen. Kies kleinere tafel(s).";
                    return RedirectToAction(nameof(TafelToewijzen), new { reservatieId, datum });
                }

                if (reservatie.AantalPersonen > totaleCapaciteit)
                {
                    TempData["ErrorMessage"] = $"De geselecteerde tafel(s) hebben een maximale capaciteit van {totaleCapaciteit} personen, maar de reservatie is voor {reservatie.AantalPersonen} personen. Selecteer grotere of extra tafel(s).";
                    return RedirectToAction(nameof(TafelToewijzen), new { reservatieId, datum });
                }

                // Controleer of geselecteerde tafels beschikbaar zijn (niet al bezet door andere aanwezige gasten)
                var reservatiesVoorTijdslot = await _unitOfWork.ReservatiesRepository
                    .GetReservatiesVoorDatumEnTijdslotAsync(datum, reservatie.TijdSlotId);
                
                var bezeteTafelIds = reservatiesVoorTijdslot
                    .Where(r => r.IsAanwezig && r.Id != reservatieId)
                    .SelectMany(r => r.Tafellijsten.Select(tl => tl.TafelId))
                    .Distinct()
                    .ToList();

                var conflicten = geselecteerdeTafels.Where(t => bezeteTafelIds.Contains(t)).ToList();
                if (conflicten.Any())
                {
                    var conflictTafels = alleTafels.Where(t => conflicten.Contains(t.Id)).Select(t => t.TafelNummer);
                    TempData["ErrorMessage"] = $"De volgende tafel(s) zijn al bezet door aanwezige gasten: {string.Join(", ", conflictTafels)}";
                    return RedirectToAction(nameof(TafelToewijzen), new { reservatieId, datum });
                }

                // Controleer of geselecteerde tafels niet al gereserveerd zijn door andere reservaties in dit tijdslot
                var gereserveerdeTafelIds = reservatiesVoorTijdslot
                    .Where(r => r.Id != reservatieId)
                    .SelectMany(r => r.Tafellijsten.Select(tl => tl.TafelId))
                    .Distinct()
                    .ToList();

                var reservatieConflicten = geselecteerdeTafels.Where(t => gereserveerdeTafelIds.Contains(t)).ToList();
                if (reservatieConflicten.Any())
                {
                    var reservatieTafels = alleTafels.Where(t => reservatieConflicten.Contains(t.Id)).Select(t => t.TafelNummer);
                    TempData["ErrorMessage"] = $"De volgende tafel(s) zijn al gereserveerd door andere gasten in dit tijdslot: {string.Join(", ", reservatieTafels)}";
                    return RedirectToAction(nameof(TafelToewijzen), new { reservatieId, datum });
                }

                // Verwijder oude tafeltoewijzingen uit database
                var oudeTafellijsten = reservatie.Tafellijsten.ToList();
                foreach (var tafelLijst in oudeTafellijsten)
                {
                    _unitOfWork.TafelLijstenRepository.Delete(tafelLijst);
                }

                // Voeg nieuwe tafeltoewijzingen toe
                foreach (var tafelId in geselecteerdeTafels)
                {
                    var nieuweTafelLijst = new TafelLijst
                    {
                        ReservatieId = reservatie.Id,
                        TafelId = tafelId
                    };
                    await _unitOfWork.TafelLijstenRepository.AddAsync(nieuweTafelLijst);
                }

                // Markeer als aanwezig
                reservatie.IsAanwezig = true;
                _unitOfWork.ReservatiesRepository.Update(reservatie);
                
                await _unitOfWork.SaveChangesAsync();

                var tafelNummers = string.Join(", ", geselecteerdeTafelObjecten.Select(t => t.TafelNummer));

                _logger.LogInformation($"Reservatie {reservatieId} ({reservatie.AantalPersonen} personen) toegewezen aan tafel(s) {tafelNummers} (capaciteit: {minimaleCapaciteit}-{totaleCapaciteit} personen) en gemarkeerd als aanwezig");
                TempData["SuccessMessage"] = $"Klant succesvol toegewezen aan tafel(s) {tafelNummers} en gemarkeerd als aanwezig.";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij toewijzen tafels voor reservatie {reservatieId}");
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het toewijzen van tafels.";
            }

            return RedirectToAction(nameof(Index), new { datum });
        }
        
        // POST: Annuleer reservatie
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
        public async Task<IActionResult> AnnuleerReservatie(int reservatieId, DateTime datum)
        {
            try
            {
                var reservatie = await _unitOfWork.ReservatiesRepository.GetReservatieWithUserAsync(reservatieId);
                
                if (reservatie == null)
                {
                    TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                    return RedirectToAction(nameof(Index), new { datum });
                }

                var klantnaam = $"{reservatie.CustomUser.Voornaam} {reservatie.CustomUser.Achternaam}";
                var tijdslot = reservatie.Tijdslot.Naam;
                var tafelNummers = reservatie.Tafellijsten.Any() 
                    ? string.Join(", ", reservatie.Tafellijsten.Select(tl => tl.Tafel.TafelNummer))
                    : "Geen";
                
                // Verwijder tafeltoewijzingen eerst (indien aanwezig)
                var tafellijsten = reservatie.Tafellijsten.ToList();
                foreach (var tafelLijst in tafellijsten)
                {
                    _unitOfWork.TafelLijstenRepository.Delete(tafelLijst);
                }

                // Verwijder bestellingen eerst (indien aanwezig)
                var bestellingen = reservatie.Bestellingen?.ToList();
                if (bestellingen != null && bestellingen.Any())
                {
                    foreach (var bestelling in bestellingen)
                    {
                        _unitOfWork.BestellingRepository.Delete(bestelling);
                    }
                }

                // Verwijder de reservatie
                _unitOfWork.ReservatiesRepository.Delete(reservatie);
                
                await _unitOfWork.SaveChangesAsync();

                var successMessage = tafelNummers != "Geen"
                    ? $"Reservatie van {klantnaam} succesvol geannuleerd. Tafel(s) {tafelNummers} zijn vrijgegeven."
                    : $"Reservatie van {klantnaam} succesvol geannuleerd.";

                _logger.LogInformation($"Reservatie {reservatieId} geannuleerd door zaalverantwoordelijke. Klant: {klantnaam}, Tijdslot: {tijdslot}, Tafels: {tafelNummers}, IsAanwezig: {reservatie.IsAanwezig}");
                TempData["SuccessMessage"] = successMessage;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Fout bij annuleren reservatie {reservatieId}");
                TempData["ErrorMessage"] = "Er is een fout opgetreden bij het annuleren van de reservatie.";
            }

            return RedirectToAction(nameof(Index), new { datum });
        }
    }
}
