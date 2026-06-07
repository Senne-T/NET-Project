using Restaurant.ViewModels.Gebruiker;
using Restaurant.ViewModels.Gebruikers;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Eigenaar)]
    public class GebruikersController : Controller
    {
        private IUnitOfWork _uow;
        private IMapper _mapper;
        private UserManager<CustomUser> _userManager;
        private RoleManager<IdentityRole> _roleManager;

        public GebruikersController(IUnitOfWork uow, IMapper mapper, UserManager<CustomUser> userManager, RoleManager<IdentityRole> roleManager)
        {
            _uow = uow;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
        }

        // GET: Gebruikers
        public async Task<IActionResult> Index()
        {
            // Medewerkers ophalen
            var actieveGebruikers = await _userManager.Users.Where(u => u.NormalizedUserName != ("VERWIJDERD_" + u.Id)).ToListAsync();

            var gebruikers = _mapper.Map<List<GebruikersViewModel>>(actieveGebruikers);

            // Rollen toevoegen
            for (int i = 0; i < gebruikers.Count; i++)
            {
                var rollen = await _userManager.GetRolesAsync(actieveGebruikers[i]);

                gebruikers[i].Rollen = rollen;
            }

            var medewerkers = gebruikers.Where(m => m.Rollen.Any() && !(m.Rollen.Count == 1 && m.Rollen.Contains("Klant"))).ToList();

            var viewModel = new GebruikersListViewModel()
            {
                Gebruikers = medewerkers
            };

            return View(viewModel);
        }

        // GET: Gebruikers
        public async Task<IActionResult> Search(GebruikersListViewModel viewModel)
        {
            // Gebruikers ophalen
            var bestaandeGebruikers = await _userManager.Users.Where(u => u.NormalizedUserName != ("VERWIJDERD_" + u.Id)).ToListAsync();

            if (!string.IsNullOrWhiteSpace(viewModel.Search))
            {
                bestaandeGebruikers = bestaandeGebruikers.Where(g =>
                    (g.Voornaam != null && g.Voornaam.Contains(viewModel.Search, StringComparison.OrdinalIgnoreCase)) ||
                    (g.Achternaam != null && g.Achternaam.Contains(viewModel.Search, StringComparison.OrdinalIgnoreCase)) ||
                    (g.Email != null && g.Email.Contains(viewModel.Search, StringComparison.OrdinalIgnoreCase))
                ).ToList();
            }

            var gebruikers = _mapper.Map<List<GebruikersViewModel>>(bestaandeGebruikers);

            // Rollen toevoegen
            for (int i = 0; i < gebruikers.Count; i++)
            {
                var rollen = await _userManager.GetRolesAsync(bestaandeGebruikers[i]);

                gebruikers[i].Rollen = rollen;
            }

            // Filters toepassen
            if (viewModel.Type == "Werknemers")
            {
                gebruikers = gebruikers.Where(g => g.Rollen.Any() && !(g.Rollen.Count == 1 && g.Rollen.Contains("Klant"))).ToList();
            }
            else if (viewModel.Type == "Klanten")
            {
                gebruikers = gebruikers.Where(g => g.Rollen.Count == 1 && g.Rollen.Contains("Klant")).ToList();
            }
            else if (viewModel.Type == "Geen rol")
            {
                gebruikers = gebruikers.Where(g => g.Rollen.Count < 1).ToList();
            }

            if (viewModel.Actief.HasValue)
            {
                gebruikers = gebruikers.Where(g => g.Actief == viewModel.Actief).ToList();
            }

            viewModel.Gebruikers = gebruikers;
            return View(nameof(Index), viewModel);
        }

        // GET: Gebruiker/Create
        public async Task<IActionResult> Create()
        {
            var viewModel = new GebruikersCreateViewModel();

            // Selectlist met landen opvullen
            await FillSelectListCreate(viewModel);

            return View(viewModel);
        }

        // POST: Gebruiker/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(GebruikersCreateViewModel viewModel)
        {
            // Validatie
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Er zit een fout in het formulier");
                await FillSelectListCreate(viewModel);

                return View(viewModel);
            }

            // Bekijken of emailadres al gebruikt wordt
            CustomUser? gebruiker = await _userManager.FindByEmailAsync(viewModel.Emailadres);

            if (gebruiker != null)
            {
                ModelState.AddModelError("", "Er bestaat al een gebruiker met dit emailadres");
                await FillSelectListCreate(viewModel);

                return View(viewModel);
            }

            // Mappen
            CustomUser nieuweGebruiker = _mapper.Map<CustomUser>(viewModel);
            nieuweGebruiker.Actief = true;
            nieuweGebruiker.EmailConfirmed = true;
            nieuweGebruiker.PhoneNumberConfirmed = true;

            // Rollen controleren
            var rollen = viewModel.RollenId?.Where(r => !string.IsNullOrWhiteSpace(r)).Distinct().ToList();

            if (rollen == null || !rollen.Any())
            {
                ModelState.AddModelError("", "Selecteer minstens 1 rol.");
                await FillSelectListCreate(viewModel);
                return View(viewModel);
            }

            // Proberen gebruiker te creëren
            var result = await _userManager.CreateAsync(nieuweGebruiker, viewModel.Password);
            if (result.Succeeded)
            {
                foreach (var rol in rollen)
                {
                    await _userManager.AddToRoleAsync(nieuweGebruiker, rol);
                }

                return RedirectToAction(nameof(Index));
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                // Input ongeldig => Stuur gebruiker opnieuw naar GET met volle Selectlist
                await FillSelectListCreate(viewModel);

                return View(viewModel);
            }
        }

        // GET: Gebruikers/Details/id
        public async Task<IActionResult> Details(string id)
        {
            // Gebruiker ophalen
            CustomUser? geselecteerdeGebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (geselecteerdeGebruiker == null)
            {
                TempData["Error"] = "Geen gebruiker gevonden.";
                return RedirectToAction("Details", new { id });
            }

            // Gebruiker mappen
            var viewModel = _mapper.Map<GebruikersDetailsViewModel>(geselecteerdeGebruiker);
            await LandOphalen(viewModel);

            // Rollen opvullen
            var rollen = await _userManager.GetRolesAsync(geselecteerdeGebruiker);
            viewModel.Rollen = rollen;

            return View(viewModel);
        }

        // GET: Gebruikers/Beheren/id
        public async Task<IActionResult> Beheren(string id)
        {
            // Gebruiker ophalen
            CustomUser? geselecteerdeGebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (geselecteerdeGebruiker == null)
            {
                TempData["Error"] = "Geen gebruiker gevonden.";
                return RedirectToAction("Beheren", new { id });
            }

            // Gebruiker mappen
            var viewModel = _mapper.Map<GebruikersEditViewModel>(geselecteerdeGebruiker);
            await FillSelectListsBeheren(viewModel, id, geselecteerdeGebruiker);

            return View(viewModel);
        }

        // POST: Gebruiker/Beheren/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Beheren(string id, GebruikersEditViewModel viewModel)
        {
            // Validatie
            if (id != viewModel.Id)
            {
                ModelState.AddModelError("", "De accountId's komen niet overeen.");
                await FillSelectListsBeheren(viewModel, id);

                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                await FillSelectListsBeheren(viewModel, id);

                return View(viewModel);
            }

            // Bekijken of emailadres al gebruikt wordt
            // GEBRUIK FirstOrDefaultAsync IN PLAATS VAN FindByEmailAsync om "Sequence contains more than one element" te vermijden
            CustomUser? bestaandGebruiker = await _userManager.Users
                .Where(u => u.NormalizedEmail == viewModel.Emailadres.ToUpper())
                .FirstOrDefaultAsync();

            if (bestaandGebruiker != null && bestaandGebruiker.Id != viewModel.Id)
            {
                ModelState.AddModelError("Emailadres", "Dit e-mailadres is al in gebruik door een andere gebruiker.");
                await FillSelectListsBeheren(viewModel, id);

                return View(viewModel);
            }

            // Originele gebruiker ophalen
            CustomUser gebruiker = await _userManager.FindByIdAsync(id);

            if (gebruiker == null)
            {
                ModelState.AddModelError("", "De gebruiker kon niet worden gevonden.");
                await FillSelectListsBeheren(viewModel, id);

                return View(viewModel);
            }

            // Gegevens aanpassen
            gebruiker.UserName = viewModel.Emailadres;
            gebruiker.NormalizedUserName = viewModel.Emailadres.ToUpper();
            gebruiker.Voornaam = viewModel.Voornaam;
            gebruiker.Achternaam = viewModel.Achternaam;
            gebruiker.Email = viewModel.Emailadres;
            gebruiker.NormalizedEmail = viewModel.Emailadres.ToUpper();
            gebruiker.Adres = viewModel.Adres;
            gebruiker.Huisnummer = viewModel.Huisnummer;
            gebruiker.Gemeente = viewModel.Gemeente;
            gebruiker.Postcode = viewModel.Postcode;
            gebruiker.LandId = viewModel.LandId;

            // Gebruiker updaten
            try
            {
                var result = await _userManager.UpdateAsync(gebruiker);

                var rolToevoegenResult = IdentityResult.Success;
                if (viewModel.RolToevoegenId != null)
                {
                    rolToevoegenResult = await _userManager.AddToRoleAsync(gebruiker, viewModel.RolToevoegenId);
                }

                var rolVerwijderenResult = IdentityResult.Success;
                if (viewModel.RolVerwijderenId != null)
                {
                    rolVerwijderenResult = await _userManager.RemoveFromRoleAsync(gebruiker, viewModel.RolVerwijderenId);
                }

                if (!result.Succeeded || !rolToevoegenResult.Succeeded || !rolVerwijderenResult.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    foreach (var error in rolToevoegenResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    foreach (var error in rolVerwijderenResult.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    await FillSelectListsBeheren(viewModel, id);

                    return View(viewModel);
                }

                TempData["Success"] = "Gebruiker succesvol aangepast.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Er is een probleem opgetreden bij het wegschrijven naar de database. Probeer het opnieuw.");
                await FillSelectListsBeheren(viewModel, id);

                return View(viewModel);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", $"Er is een onverwachte fout opgetreden: {ex.Message}");
                await FillSelectListsBeheren(viewModel, id);

                return View(viewModel);
            }
        }

        // GET: Gebruikers/Deactiveren/id
        public async Task<IActionResult> Deactiveren(string id)
        {
            // Gebruiker ophalen
            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                TempData["Error"] = "Geen account gevonden.";
                return RedirectToAction(nameof(Index));
            }

            // Controleren of gebruiker de eigenaar is
            if (await _userManager.IsInRoleAsync(gebruiker, StaticRollen.Eigenaar))
            {
                TempData["Error"] = "Als eigenaar kan je uw account niet deactiveren.";
                return RedirectToAction(nameof(Index));
            }

            var viewModel = _mapper.Map<GebruikersDeactiverenViewModel>(gebruiker);
            return View(viewModel);
        }

        // POST: Gebruikers/Deactiveren/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeactiverenConfirmed(string id)
        {
            // Gebruiker ophalen
            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                TempData["Error"] = "Geen account gevonden.";
                return RedirectToAction(nameof(Index));
            }

            // Controleren of gebruiker de eigenaar is
            if (await _userManager.IsInRoleAsync(gebruiker, StaticRollen.Eigenaar))
            {
                TempData["Error"] = "Als eigenaar kan je uw account niet deactiveren.";
                return RedirectToAction(nameof(Index));
            }

            if (gebruiker.Actief)
            {
                gebruiker.Actief = false;
                TempData["Success"] = "Gebruiker succesvol gedeactiveerd.";
            }
            else
            {
                gebruiker.Actief = true;
                TempData["Success"] = "Gebruiker succesvol geactiveerd.";
            }

            // Gebruiker updaten
            try
            {
                // Gebruiker updaten
                var result = await _userManager.UpdateAsync(gebruiker);

                if (!result.Succeeded)
                {
                    TempData["Error"] = "";
                    foreach (var error in result.Errors)
                    {
                        TempData["Error"] += $"{error.Description}\n";
                    }
                    return RedirectToAction(nameof(Index));
                }

                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException e)
            {
                TempData["Error"] = e;
                return RedirectToAction(nameof(Index));
            }
        }

        // GET: Gebruikers/Delete/id
        public async Task<IActionResult> Delete(string id)
        {
            // Gebruiker ophalen
            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                TempData["Error"] = "Geen account gevonden.";
                return RedirectToAction(nameof(Index));
            }

            // Controleren of gebruiker de eigenaar is
            if (await _userManager.IsInRoleAsync(gebruiker, StaticRollen.Eigenaar))
            {
                TempData["Error"] = "Als eigenaar kan je uw account niet verwijderen.";
                return RedirectToAction(nameof(Index));
            }

            GebruikerDeleteViewModel viewModel = _mapper.Map<GebruikerDeleteViewModel>(gebruiker);
            return View(viewModel);
        }

        // POST: Gebruikers/DeleteConfirmed/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            // Gebruiker ophalen
            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                TempData["Error"] = "Geen account gevonden.";
                return RedirectToAction(nameof(Index));
            }

            // Controleren of gebruiker de eigenaar is
            if (await _userManager.IsInRoleAsync(gebruiker, StaticRollen.Eigenaar))
            {
                TempData["Error"] = "Als eigenaar kan je uw account niet verwijderen.";
                return RedirectToAction(nameof(Index));
            }

            gebruiker.Actief = false;
            gebruiker.Voornaam = null;
            gebruiker.Achternaam = null;
            gebruiker.Email = null;
            gebruiker.NormalizedEmail = null;
            gebruiker.UserName = $"VERWIJDERD_{gebruiker.Id}";
            gebruiker.NormalizedUserName = $"VERWIJDERD_{gebruiker.Id}";
            gebruiker.PhoneNumber = null;
            gebruiker.Adres = null;
            gebruiker.Huisnummer = null;
            gebruiker.Gemeente = null;
            gebruiker.Postcode = null;
            gebruiker.LandId = -1;

            var rollen = await _userManager.GetRolesAsync(gebruiker);

            // Gebruiker updaten
            try
            {
                // Rollen verwijderen
                var rollenResult = IdentityResult.Success;
                if (rollen.Count > 0)
                {
                    rollenResult = await _userManager.RemoveFromRolesAsync(gebruiker, rollen);
                }

                // Gebruiker updaten
                var result = await _userManager.UpdateAsync(gebruiker);

                if (!result.Succeeded || !rollenResult.Succeeded)
                {
                    TempData["Error"] = "";
                    foreach (var error in result.Errors)
                    {
                        TempData["Error"] += $"{error.Description}\n";
                    }
                    return RedirectToAction(nameof(Index));
                }

                TempData["Success"] = "Gebruiker succesvol verwijderd.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateConcurrencyException e)
            {
                TempData["Error"] = e;
                return RedirectToAction(nameof(Index));
            }
        }

        private async Task LandOphalen(GebruikersDetailsViewModel viewModel)
        {
            // Data inladen
            if (viewModel.LandId != null)
            {
                Land land = await _uow.LandRepository.GetByIdAsync(viewModel.LandId);
                viewModel.Land = land.Naam;
            }
        }

        private async Task FillSelectListCreate(GebruikersCreateViewModel viewModel)
        {
            // Data inladen
            IEnumerable<Land> landen = await _uow.LandRepository.GetAllAsync();
            var rollen = await _roleManager.Roles.Select(r => r.Name).ToListAsync();

            // Data in selectlist plaatsen
            viewModel.Landen = landen.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Naam
            }).ToList();

            viewModel.AlleRollen = rollen.Select(x => new SelectListItem
            {
                Value = x,
                Text = x
            }).ToList();
        }

        private async Task FillSelectListsBeheren(GebruikersEditViewModel viewModel, string id, CustomUser? gebruiker = null)
        {
            // Data inladen
            IEnumerable<Land> landen = await _uow.LandRepository.GetAllAsync();

            // Rollen opvullen
            if (gebruiker == null)
            {
                gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

                if (gebruiker == null)
                {
                    throw new Exception("Geen gebruiker gevonden.");
                }
            }

            var huidigeRollen = await _userManager.GetRolesAsync(gebruiker);

            // Data in landen-selectlist plaatsen
            viewModel.Landen = landen.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Naam
            }).ToList();

            viewModel.HuidigeRollen = huidigeRollen.Select(x => new SelectListItem
            {
                Value = x,
                Text = x
            }).ToList();

            var alleRollen = _roleManager.Roles.Select(r => r.Name).ToList();

            foreach (var rol in huidigeRollen)
            {
                alleRollen.Remove(rol);
            }

            viewModel.AlleRollen = alleRollen.Select(x => new SelectListItem
            {
                Value = x,
                Text = x
            }).ToList();
        }
    }
}
