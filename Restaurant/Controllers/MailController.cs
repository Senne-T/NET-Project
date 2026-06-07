using Restaurant.Services;
using Restaurant.ViewModels.Mail;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Eigenaar)]
    public class MailController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly MailService _mailService;
        private readonly IMapper _mapper;

        public MailController(IUnitOfWork unitOfWork, MailService mailService, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mailService = mailService;
            _mapper = mapper;
        }

        // GET: Mail
        public async Task<IActionResult> Index()
        {
            var mails = await _unitOfWork.MailRepository.GetMailAsync();
            var viewModels = _mapper.Map<List<MailListViewModel>>(mails);
            return View(viewModels);
        }

        // GET: Mail/Create
        public IActionResult Create()
        {
            return View(new MailViewModel());
        }

        // POST: Mail/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(MailViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            var mail = _mapper.Map<Mail>(viewModel);
            _unitOfWork.MailRepository.Add(mail);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "E-mailsjabloon succesvol aangemaakt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Mail/Edit/id
        public async Task<IActionResult> Edit(int id)
        {
            var mail = await _unitOfWork.MailRepository.GetMailByIdAsync(id);

            if (mail == null)
                return NotFound();

            var viewModel = _mapper.Map<MailViewModel>(mail);
            return View(viewModel);
        }

        // POST: Mail/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, MailViewModel viewModel)
        {
            if (id != viewModel.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(viewModel);

            var mail = await _unitOfWork.MailRepository.GetMailByIdAsync(id);
            if (mail == null)
                return NotFound();

            _mapper.Map(viewModel, mail);
            _unitOfWork.MailRepository.Update(mail);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "E-mailsjabloon succesvol bijgewerkt.";
            return RedirectToAction(nameof(Index));
        }

        // POST: Mail/Delete/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var mail = await _unitOfWork.MailRepository.GetMailByIdAsync(id);

            if (mail == null)
                return NotFound();

            _unitOfWork.MailRepository.Remove(mail);
            await _unitOfWork.SaveChangesAsync();

            TempData["SuccessMessage"] = "E-mailsjabloon succesvol verwijderd.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Mail/Test
        // TestMailpagina tonen
        public async Task<IActionResult> Test(int id)
        {
            var mail = await _unitOfWork.MailRepository.GetMailByIdAsync(id);

            if (mail == null)
                return NotFound();

            var model = new TestMailViewModel
            {
                TemplateId = mail.Id,
                TemplateNaam = mail.Onderwerp
            };

            return View(model);
        }

        // POST: Mail/Test
        // TestMail verzenden
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Test(TestMailViewModel model)
        {
            if (!ModelState.IsValid)
            {
                var mail = await _unitOfWork.MailRepository.GetMailByIdAsync(model.TemplateId);
                if (mail != null)
                {
                    model.TemplateNaam = mail.Naam;
                }
                return View(model);
            }

            var mailTemplate = await _unitOfWork.MailRepository.GetMailByIdAsync(model.TemplateId);

            if (mailTemplate == null)
                return NotFound();

            await _mailService.SendMailAsync(
                model.TestEmail!,
                mailTemplate.Onderwerp ?? "(geen onderwerp)",
                mailTemplate.Body ?? string.Empty,
                null
            );

            TempData["SuccessMessage"] = $"Testmail verzonden naar {model.TestEmail}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
