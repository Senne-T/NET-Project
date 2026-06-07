using MVCDemo.ViewModels.Product;
using Restaurant.ViewModels.Producten;

namespace Restaurant.Controllers
{
    [Authorize(Roles = StaticRollen.Kok)]
    public class SpecialiteitenController : Controller
    {
        private IUnitOfWork _uow;
        private IMapper _mapper;

        public SpecialiteitenController(IUnitOfWork unitOfWork, IMapper mapper)
        {
            _uow = unitOfWork;
            _mapper = mapper;
        }

        private async Task<IEnumerable<SelectListItem>> BuildCategorieSelectListAsync(int typeId)
        {
            var categories = await _uow.ProductRepository.GetCategoriesByTypeAsync(typeId);

            return categories.Select(c => new SelectListItem
            {
                Value = c.Id.ToString(),
                Text = c.Naam
            });
        }

        // GET: Specialiteiten
        public async Task<IActionResult> Index()
        {
            // Haal de producten op van CategorieType met TypeId = 4 (Specialiteit)
            IEnumerable<Product> producten = await _uow.ProductRepository.GetProductsByCategorieTypeAsync(4);


            ProductListViewModel model = new ProductListViewModel();
            model.Product = _mapper.Map<List<ProductViewModel>>(producten);
            return View(model);
        }

        // GET: Specialiteiten/Details/id
        public async Task<IActionResult> Details(int id)
        {
            Product model = await _uow.ProductRepository.GetByIdAsync(id);

            if (model == null || model.Categorie == null || model.Categorie.TypeId != 4)
            {
                return RedirectToAction(nameof(Index));
            }

            ProductDetailsViewModel viewmodel = _mapper.Map<ProductDetailsViewModel>(model);
            return View(viewmodel);
        }

        // GET: Specialiteiten/Create
        public async Task<IActionResult> Create()
        {
            var model = new ProductCreateViewModel
            {
                Actief = true,
                Categorieën = await BuildCategorieSelectListAsync(4)
            };

            return View(model);
        }

        // POST: Specialiteiten/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProductCreateViewModel viewmodel)
        {
            if (!ModelState.IsValid)
            {
                viewmodel.Categorieën = await BuildCategorieSelectListAsync(4);
                return View(viewmodel);
            }

            var product = _mapper.Map<Product>(viewmodel);

            await _uow.ProductRepository.AddAsync(product);
            await _uow.SaveChangesAsync();

            // Prijs aanmaken (kan niet met AutoMapper, hoort manueel)
            var prijs = new PrijsProduct
            {
                ProductId = product.Id,
                Prijs = viewmodel.Prijs,
                DatumVanaf = DateTime.Now
            };

            await _uow.ProductRepository.AddPrijsAsync(prijs);
            await _uow.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Specialiteiten/Edit/id
        public async Task<IActionResult> Edit(int id)
        {
            var product = await _uow.ProductRepository.GetByIdAsync(id);
            if (product == null || product.Categorie.TypeId != 4)
            {
                return RedirectToAction("Index");
            }

            var viewmodel = _mapper.Map<ProductEditViewModel>(product);

            viewmodel.Categorieën = await BuildCategorieSelectListAsync(4);


            return View(viewmodel);
        }

        // POST: Specialiteiten/Edit/id
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, ProductEditViewModel viewmodel)
        {
            if (id != viewmodel.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                viewmodel.Categorieën = await BuildCategorieSelectListAsync(4);
                return View(viewmodel);
            }

            // ► Haal bestaande product op (inclusief prijzen)
            var product = await _uow.ProductRepository.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            // ► Map ViewModel → bestaand product (update basisdata)
            _mapper.Map(viewmodel, product);

            // ───────────────────────────────────────
            //           PRIJS LOGICA
            // ───────────────────────────────────────
            decimal huidigePrijs = product.PrijsProducten
                .OrderByDescending(p => p.DatumVanaf)
                .FirstOrDefault()?.Prijs ?? 0;

            decimal nieuwePrijs = viewmodel.Prijs;

            // Decimal vergelijken: met afronding!
            if (Math.Round(huidigePrijs, 2) != Math.Round(nieuwePrijs, 2))
            {
                var prijsUpdate = new PrijsProduct
                {
                    ProductId = product.Id,
                    Prijs = nieuwePrijs,
                    DatumVanaf = DateTime.Now
                };

                await _uow.ProductRepository.AddPrijsAsync(prijsUpdate);
            }

            // ► Update product zelf
            await _uow.ProductRepository.UpdateAsync(product);
            await _uow.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: Specialiteiten/Delete/id
        public async Task<IActionResult> Delete(int id)
        {
            // Validatie voor databasecall
            if (id <= 0)
                return BadRequest("Ongeldig product ID.");

            var product = await _uow.ProductRepository.GetByIdAsync(id);
            if (product == null || product.Categorie.TypeId != 4)
            {
                return NotFound();
            }

            var viewmodel = _mapper.Map<ProductDeleteViewModel>(product);
            return View(viewmodel);
        }

        // POST: Specialiteiten/Delete/id
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {

            // Validatie voor databasecall
            if (id <= 0)
                return BadRequest("Ongeldig product ID.");

            // Product ophalen inclusief PrijsProducten
            var product = await _uow.ProductRepository.GetByIdAsync(id);
            if (product == null)
                return NotFound();

            // Eerst alle gerelateerde prijzen verwijderen
            foreach (var prijs in product.PrijsProducten.ToList())
            {
                await _uow.ProductRepository.DeletePrijsAsync(prijs.Id);
            }

            // Dan het product verwijderen
            await _uow.ProductRepository.DeleteAsync(id);

            // Alles opslaan
            await _uow.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
