using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Restaurant.Models;
using Restaurant.ViewModels.Tafel;
using Restaurant.Data.UnitOfWork;
using AutoMapper;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Zaalverantwoordelijke + "," + StaticRollen.Eigenaar)]
    public class TafelbeheerController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;
        private readonly ILogger<TafelbeheerController> _logger;

        public TafelbeheerController(IUnitOfWork unitOfWork, IMapper mapper, ILogger<TafelbeheerController> logger)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
            _logger = logger;
        }

        // GET: Tafelbeheer
        public async Task<IActionResult> Index()
        {
            var tafels = await _unitOfWork.TafelRepository.GetAllTafelsAsync();
            
            var model = new TafelListViewModel
            {
                Tafels = _mapper.Map<List<TafelViewModel>>(tafels)
            };

            return View(model);
        }

        // GET: Tafelbeheer/Details/5
        public async Task<IActionResult> Details(int id)
        {
            if (id <= 0)
                return BadRequest("Ongeldig tafel ID.");

            var tafel = await _unitOfWork.TafelRepository.GetByIdAsync(id);

            if (tafel == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<TafelDetailsViewModel>(tafel);
            return View(viewModel);
        }

        // GET: Tafelbeheer/Create
        public IActionResult Create()
        {
            var model = new TafelCreateViewModel
            {
                Actief = true
            };

            return View(model);
        }

        // POST: Tafelbeheer/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TafelCreateViewModel viewModel)
        {
            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            if (viewModel.MinAantalPersonen > viewModel.AantalPersonen)
            {
                ModelState.AddModelError("MinAantalPersonen", "Minimum aantal personen mag niet groter zijn dan maximum aantal personen.");
                return View(viewModel);
            }

            var tafel = _mapper.Map<Models.Tafel>(viewModel);

            await _unitOfWork.TafelRepository.AddAsync(tafel);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Tafel {tafel.TafelNummer} aangemaakt met capaciteit {tafel.MinAantalPersonen}-{tafel.AantalPersonen} personen");

            TempData["SuccessMessage"] = $"Tafel {tafel.TafelNummer} succesvol aangemaakt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Tafelbeheer/Edit/5
        public async Task<IActionResult> Edit(int id)
        {
            if (id <= 0)
                return BadRequest("Ongeldig tafel ID.");

            var tafel = await _unitOfWork.TafelRepository.GetByIdAsync(id);
            
            if (tafel == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<TafelEditViewModel>(tafel);
            return View(viewModel);
        }

        // POST: Tafelbeheer/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, TafelEditViewModel viewModel)
        {
            if (id != viewModel.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                return View(viewModel);
            }

            if (viewModel.MinAantalPersonen > viewModel.AantalPersonen)
            {
                ModelState.AddModelError("MinAantalPersonen", "Minimum aantal personen mag niet groter zijn dan maximum aantal personen.");
                return View(viewModel);
            }

            var tafel = await _unitOfWork.TafelRepository.GetByIdAsync(id);
            if (tafel == null)
                return NotFound();

            _mapper.Map(viewModel, tafel);

            await _unitOfWork.TafelRepository.UpdateAsync(tafel);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Tafel {tafel.TafelNummer} bijgewerkt met capaciteit {tafel.MinAantalPersonen}-{tafel.AantalPersonen} personen");

            TempData["SuccessMessage"] = $"Tafel {tafel.TafelNummer} succesvol bijgewerkt.";
            return RedirectToAction(nameof(Index));
        }

        // GET: Tafelbeheer/Delete/5
        public async Task<IActionResult> Delete(int id)
        {
            if (id <= 0)
                return BadRequest("Ongeldig tafel ID.");

            var tafel = await _unitOfWork.TafelRepository.GetByIdAsync(id);
            
            if (tafel == null)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<TafelDeleteViewModel>(tafel);
            return View(viewModel);
        }

        // POST: Tafelbeheer/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            if (id <= 0)
                return BadRequest("Ongeldig tafel ID.");

            var tafel = await _unitOfWork.TafelRepository.GetByIdAsync(id);
            
            if (tafel == null)
                return NotFound();

            var tafelNummer = tafel.TafelNummer;

            var tafelInGebruik = await _unitOfWork.TafelLijstenRepository.Find(
                tl => tl.TafelId == id,
                tl => tl.Reservatie
            );

            if (tafelInGebruik.Any())
            {
                TempData["ErrorMessage"] = $"Tafel {tafelNummer} kan niet verwijderd worden omdat er nog reservaties aan gekoppeld zijn. Deactiveer de tafel in plaats van deze te verwijderen.";
                return RedirectToAction(nameof(Index));
            }

            await _unitOfWork.TafelRepository.DeleteAsync(id);
            await _unitOfWork.SaveChangesAsync();

            _logger.LogInformation($"Tafel {tafelNummer} verwijderd uit database");

            TempData["SuccessMessage"] = $"Tafel {tafelNummer} succesvol verwijderd.";
            return RedirectToAction(nameof(Index));
        }
    }
}
