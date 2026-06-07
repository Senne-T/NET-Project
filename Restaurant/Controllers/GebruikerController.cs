using Restaurant.Services;
using Restaurant.ViewModels.Gebruiker;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Klant)]
    public class GebruikerController : Controller
    {
        private readonly IUnitOfWork _uow;
        private readonly IMapper _mapper;
        private readonly UserManager<CustomUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<CustomUser> _signInManager;
        private readonly MailService _mailService;

        public GebruikerController(
            IUnitOfWork uow,
            IMapper mapper,
            UserManager<CustomUser> userManager,
            RoleManager<IdentityRole> roleManager,
            SignInManager<CustomUser> signInManager,
            MailService mailService)
        {
            _uow = uow;
            _mapper = mapper;
            _userManager = userManager;
            _roleManager = roleManager;
            _signInManager = signInManager;
            _mailService = mailService;
        }

        // GET: Gebruiker/Register
        [AllowAnonymous]
        public async Task<IActionResult> Register()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new GebruikerRegisterViewModel();

            // Selectlist met landen opvullen
            await FillSelectListRegister(viewModel);

            return View(viewModel);
        }

        // POST: Gebruiker/Register
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(GebruikerRegisterViewModel viewModel)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            // Validatie
            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Er zit een fout in het formulier");
                await FillSelectListRegister(viewModel);

                return View(viewModel);
            }

            // Bekijken of emailadres al gebruikt wordt
            CustomUser? gebruiker = await _userManager.FindByEmailAsync(viewModel.Emailadres);

            if (gebruiker != null)
            {
                ModelState.AddModelError("", "Er bestaat al een gebruiker met dit emailadres");
                await FillSelectListRegister(viewModel);

                return View(viewModel);
            }

            // Mappen
            CustomUser user = _mapper.Map<CustomUser>(viewModel);
            user.Actief = true;
            user.EmailConfirmed = true;
            user.PhoneNumberConfirmed = true;

            // Proberen te registreren
            var result = await _userManager.CreateAsync(user, viewModel.Password);
            if (result.Succeeded)
            {
                // Klantrol toevoegen
                CustomUser? newUser = await _userManager.FindByEmailAsync(viewModel.Emailadres);
                IdentityRole? role = await _roleManager.FindByNameAsync(StaticRollen.Klant);

                await _userManager.AddToRoleAsync(newUser, role.Name);

                // Automatisch aanmelden
                await _signInManager.PasswordSignInAsync(newUser, viewModel.Password, false, false);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                // Input ongeldig => Stuur gebruiker opnieuw naar GET met volle Selectlist
                await FillSelectListRegister(viewModel);

                return View(viewModel);
            }
        }

        // GET: Gebruiker/Login
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            return View();
        }

        // POST: Gebruiker/Login
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(GebruikerLoginViewModel viewModel)
        {
            if (_signInManager.IsSignedIn(User))
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Er zit een fout in het formulier");
                return View(viewModel);
            }

            // Gebruiker e-mailadres controleren
            CustomUser user = await _userManager.FindByNameAsync(viewModel.Emailadres);
            if (user == null)
            {
                ModelState.AddModelError("", "E-mailadres of wachtwoord ongeldig.");
                return View(viewModel);
            }
            else if (!user.EmailConfirmed)
            {
                ModelState.AddModelError("", "E-mailadres is nog niet geverifiëerd");
                return View(viewModel);
            }

            // Wachtwoord controleren
            if (!await _userManager.CheckPasswordAsync(user, viewModel.Password))
            {
                ModelState.AddModelError("", "E-mailadres of wachtwoord ongeldig.");
                return View(viewModel);
            }

            if (!user.Actief)
            {
                ModelState.AddModelError("", "Account is inactief, neem contact op.");
                return View(viewModel);
            }

            var result = await _signInManager.PasswordSignInAsync(user, viewModel.Password, false, false);
            if (result.IsLockedOut)
            {
                ModelState.AddModelError("", "Account is geblokkeerd.");
                return View(viewModel);
            }

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }
            else
            {
                ModelState.AddModelError("", "Login mislukt.");
                return View(viewModel);
            }
        }

        [AllowAnonymous]
        // GET: Gebruiker/Logout
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // GET: Gebruiker/Beheren/id
        [Authorize(Roles = StaticRollen.Klant)]
        public async Task<IActionResult> Beheren(string id)
        {
            // Validatie
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            CustomUser? user = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (user == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = _mapper.Map<GebruikerEditViewModel>(user);

            await FillSelectListBeheren(viewModel);

            return View(viewModel);
        }

        // POST: Gebruiker/Beheren/id
        [Authorize(Roles = StaticRollen.Klant)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Beheren(string id, GebruikerEditViewModel viewModel)
        {
            // Validatie
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            if (id != viewModel.Id)
            {
                ModelState.AddModelError("", "De accountId's komen niet overeen.");
                await FillSelectListBeheren(viewModel);

                return View(viewModel);
            }

            if (!ModelState.IsValid)
            {
                await FillSelectListBeheren(viewModel);

                return View(viewModel);
            }

            // Bekijken of emailadres al gebruikt wordt
            CustomUser? bestaandGebruiker = await _userManager.FindByEmailAsync(viewModel.Emailadres);

            if (bestaandGebruiker != null && bestaandGebruiker.Id != viewModel.Id)
            {
                ModelState.AddModelError("", "Er bestaat al een gebruiker met dit emailadres");
                await FillSelectListBeheren(viewModel);

                return View(viewModel);
            }

            // Originele gebruiker ophalen
            CustomUser gebruiker = await _userManager.FindByIdAsync(id);

            if (gebruiker == null)
            {
                ModelState.AddModelError("", "De gebruiker kon niet worden gevonden.");
                await FillSelectListBeheren(viewModel);

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

                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError("", error.Description);
                    }
                    await FillSelectListBeheren(viewModel);

                    return View(viewModel);
                }

                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateConcurrencyException)
            {
                ModelState.AddModelError("", "Er is een probleem opgetreden bij het wegschrijven naar de database. Probeer het opnieuw.");
                await FillSelectListBeheren(viewModel);

                return View(viewModel);
            }
        }

        // GET: Gebruiker/WachtwoordBeheren
        [Authorize(Roles = StaticRollen.Klant)]
        public IActionResult WachtwoordBeheren(string id)
        {
            // Validatie
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            var viewModel = new GebruikerWachtwoordBeherenViewModel();
            viewModel.Id = id;

            return View(viewModel);
        }

        // POST: Gebruiker/WachtwoordBeheren
        [Authorize(Roles = StaticRollen.Klant)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WachtwoordBeheren(string id, GebruikerWachtwoordBeherenViewModel viewModel)
        {
            // Validatie
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            if (!ModelState.IsValid)
            {
                ModelState.AddModelError("", "Er zit een fout in het formulier");

                return View(viewModel);
            }

            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                ModelState.AddModelError("", "Gebruiker niet gevonden.");

                return View(viewModel);
            }

            var originalPasswordResult = await _userManager.CheckPasswordAsync(gebruiker, viewModel.OriginalPassword);
            if (!originalPasswordResult)
            {
                ModelState.AddModelError("", "Huidig wachtwoord is verkeerd.");

                return View(viewModel);
            }

            // Wachtwoord proberen aanpassen
            var result = await _userManager.ChangePasswordAsync(gebruiker, viewModel.OriginalPassword, viewModel.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError("", error.Description);
                }

                return View(viewModel);
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: Gebruiker/Delete/id
        [Authorize(Roles = StaticRollen.Klant)]
        public async Task<IActionResult> Delete(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Controleren of gebruiker de eigenaar is
            if (await _userManager.IsInRoleAsync(gebruiker, StaticRollen.Eigenaar))
            {
                TempData["Error"] = "Als eigenaar kan je uw account niet verwijderen.";
                return RedirectToAction("Beheren", new { id });
            }

            GebruikerDeleteViewModel viewModel = _mapper.Map<GebruikerDeleteViewModel>(gebruiker);
            return View(viewModel);
        }

        // POST: Gebruiker/DeleteConfirmed/id
        [Authorize(Roles = StaticRollen.Klant)]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(string id)
        {
            var currentUserId = _userManager.GetUserId(User);

            if (currentUserId != id)
            {
                return RedirectToAction("Index", "Home");
            }

            CustomUser? gebruiker = await _userManager.Users.Where(u => u.Id == id).FirstOrDefaultAsync();

            if (gebruiker == null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Controleren of gebruiker de eigenaar is
            if (await _userManager.IsInRoleAsync(gebruiker, StaticRollen.Eigenaar))
            {
                TempData["Error"] = "Als eigenaar kan je uw account niet verwijderen.";
                return RedirectToAction("Beheren", new { id });
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
                    return RedirectToAction("Beheren", new { id });
                }

                await _signInManager.SignOutAsync();

                return RedirectToAction("Index", "Home");
            }
            catch (DbUpdateConcurrencyException e)
            {
                TempData["Error"] = e;

                return RedirectToAction("Beheren", new { id });
            }
        }

        // GET: Gebruiker/WachtwoordVergeten
        [AllowAnonymous]
        public IActionResult WachtwoordVergeten()
        {
            var vm = new WachtwoordVergetenViewModel();
            return View(vm);
        }

        // POST: Gebruiker/WachtwoordVergeten
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> WachtwoordVergeten(WachtwoordVergetenViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Emailadres);

            if (user != null && user.EmailConfirmed)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);

                var callbackUrl = Url.Action(
                    nameof(ResetWachtwoord),
                    "Gebruiker",
                    new { email = user.Email, token = token },
                    protocol: HttpContext.Request.Scheme);

                var onderwerp = "Wachtwoord resetten";
                var body = $"Beste,\n\n" +
                           $"Klik op de volgende link om uw wachtwoord te resetten:\n{callbackUrl}\n\n" +
                           $"Als u dit niet heeft aangevraagd, mag u deze mail negeren.";

                await _mailService.SendMailAsync(user.Email!, onderwerp, body);
            }

            return View("WachtwoordVergetenBevestiging");
        }

        // GET: Gebruiker/ResetWachtwoord
        [AllowAnonymous]
        public IActionResult ResetWachtwoord(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Index", "Home");
            }

            var vm = new ResetWachtwoordViewModel
            {
                Emailadres = email,
                Token = token
            };

            return View(vm);
        }

        // POST: Gebruiker/ResetWachtwoord
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetWachtwoord(ResetWachtwoordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var user = await _userManager.FindByEmailAsync(model.Emailadres);
            if (user == null)
            {
                return RedirectToAction(nameof(Login));
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NieuwWachtwoord);

            if (result.Succeeded)
            {
                TempData["SuccessMessage"] = "Uw wachtwoord is aangepast. U kan nu aanmelden.";
                return RedirectToAction(nameof(Login));
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }

        private async Task FillSelectListRegister(GebruikerRegisterViewModel viewModel)
        {
            // Data inladen
            IEnumerable<Land> landen = await _uow.LandRepository.GetAllAsync();

            // Data in selectlist plaatsen
            viewModel.Landen = landen.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Naam
            }).ToList();
        }

        private async Task FillSelectListBeheren(GebruikerEditViewModel viewModel)
        {
            // Data inladen
            IEnumerable<Land> landen = await _uow.LandRepository.GetAllAsync();

            // Data in selectlist plaatsen
            viewModel.Landen = landen.Select(x => new SelectListItem
            {
                Value = x.Id.ToString(),
                Text = x.Naam
            }).ToList();
        }
    }
}