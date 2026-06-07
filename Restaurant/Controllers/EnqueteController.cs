using Restaurant.ViewModels.Enquete;

namespace Restaurant.Controllers
{
    [Authorize]
    public class EnqueteController : Controller
    {
        private readonly IUnitOfWork _unitOfWork;
        private readonly IMapper _mapper;

        public EnqueteController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _unitOfWork = unitOfWork;
            _mapper = mapper;
        }

        [Authorize(Roles = StaticRollen.Klant)]
        public async Task<IActionResult> Index(int? reservatieId = null)
        {
            if (reservatieId.HasValue)
            {
                var reservatie = await _unitOfWork.ReservatiesRepository.GetByIdAsync(reservatieId.Value);
                if (reservatie == null)
                {
                    TempData["ErrorMessage"] = "Reservatie niet gevonden.";
                    return RedirectToAction("Index", "Reservatie");
                }
                
                if (reservatie.Datum.HasValue && DateTime.Now.Date > reservatie.Datum.Value.AddDays(14))
                {
                    TempData["ErrorMessage"] = "De enquête kan alleen ingevuld worden binnen 2 weken na de reservatie datum.";
                    return RedirectToAction("MyReservations", "Reservatie");
                }
                
                if (reservatie.EvaluatieAantalSterren != -1)
                {
                    TempData["ErrorMessage"] = "Deze reservatie is al geëvalueerd.";
                    return RedirectToAction("MyReservations", "Reservatie");
                }
            }
            
            var surveyQuestion = await _unitOfWork.ParametersRepository.GetValueByNameAsync("EnqueteVraag");
            ViewBag.SurveyQuestion = surveyQuestion ?? "Hoe tevreden was u over uw bezoek aan ons restaurant?";
            ViewBag.ReservatieId = reservatieId;
            return View();
        }

        [Authorize(Roles = StaticRollen.Klant)]
        [HttpPost]
        public async Task<IActionResult> Submit(int score, string opmerkingen, int? reservatieId = null)
        {
            // Save the response to the reservation if reservatieId is provided
            if (reservatieId.HasValue)
            {
                var reservatie = await _unitOfWork.ReservatiesRepository.GetByIdAsync(reservatieId.Value);
                if (reservatie != null)
                {
                    if (reservatie.Datum.HasValue && DateTime.Now.Date > reservatie.Datum.Value.AddDays(14))
                    {
                        TempData["ErrorMessage"] = "De enquête kan alleen ingevuld worden binnen 2 weken na de reservatie datum.";
                        return RedirectToAction("MyReservations", "Reservatie");
                    }
                    
                    reservatie.EvaluatieAantalSterren = score;
                    reservatie.EvaluatieOpmerkingen = opmerkingen;
                    await _unitOfWork.SaveChangesAsync();
                }
            }

            ViewBag.Score = score;
            ViewBag.Opmerkingen = opmerkingen;
            return View("ThankYou");
        }

        [Authorize(Roles = StaticRollen.Eigenaar)]
        public async Task<IActionResult> AlleEvaluaties(DateTime? dateFrom, DateTime? dateTo)
        {
            var evaluationsQuery = _unitOfWork.ReservatiesRepository.GetAllEvaluationsAsync();

            var evaluations = await evaluationsQuery;

            // Filter by date if provided
            if (dateFrom.HasValue)
            {
                evaluations = evaluations.Where(e => e.Datum >= dateFrom.Value.Date).ToList();
            }
            if (dateTo.HasValue)
            {
                evaluations = evaluations.Where(e => e.Datum <= dateTo.Value.Date).ToList();
            }

            var evaluationViewModels = _mapper.Map<List<EnqueteViewModel>>(evaluations);

            var totalEvaluations = evaluationViewModels.Count;
            var averageScore = totalEvaluations > 0 ? evaluationViewModels.Average(e => e.Score) : 0;

            // Calculate score distribution for chart
            var scoreDistribution = new int[6]; // 0 to 5
            foreach (var eval in evaluationViewModels)
            {
                if (eval.Score >= 0 && eval.Score <= 5)
                {
                    scoreDistribution[eval.Score]++;
                }
            }
            var nonEvaluatedReservations = await _unitOfWork.ReservatiesRepository.GetAllNonEvaluatedReservationsAsync();

            var dashboardViewModel = new EnqueteDashboardViewModel
            {
                Evaluations = evaluationViewModels,
                TotalEvaluations = totalEvaluations,
                AverageScore = averageScore,
                ScoreDistribution = scoreDistribution
            };

            ViewBag.DateFrom = dateFrom?.ToString("yyyy-MM-dd");
            ViewBag.DateTo = dateTo?.ToString("yyyy-MM-dd");
            ViewBag.NonEvaluatedReservations = nonEvaluatedReservations;

            return View(dashboardViewModel);
        }

        [Authorize(Roles = StaticRollen.Eigenaar)]
        public async Task<IActionResult> Detail(int id)
        {
            var reservatie = await _unitOfWork.ReservatiesRepository.GetReservatieWithUserAsync(id);
            if (reservatie == null || reservatie.EvaluatieAantalSterren == -1)
            {
                return NotFound();
            }

            var viewModel = _mapper.Map<EnqueteViewModel>(reservatie);

            return View(viewModel);
        }
    }
}