using Restaurant.ViewModels.Parameter;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Eigenaar)]
    public class ParameterController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public ParameterController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        // GET: Parameter
        public async Task<IActionResult> Index()
        {
            var parameters = await _unitOfWork.ParametersRepository.GetAllAsync();
            var vm = _mapper.Map<IEnumerable<ParameterViewModel>>(parameters);
            return View(vm);
        }

        // GET: Parameter/Edit/id
        public async Task<IActionResult> Edit(int id)
        {
            var model = await _unitOfWork.ParametersRepository.GetByIdAsync(id);
            if (model == null)
                return NotFound();

            var vm = _mapper.Map<ParameterViewModel>(model);
            return View(vm);
        }

        // POST: Parameter/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ParameterViewModel vm)
        {
            if (id != vm.Id)
                return BadRequest();

            if (!ModelState.IsValid)
                return View(vm);

            var model = await _unitOfWork.ParametersRepository.GetByIdAsync(id);
            if (model == null)
                return NotFound();

            model.Waarde = vm.Waarde;

            _unitOfWork.ParametersRepository.Update(model);
            await _unitOfWork.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
