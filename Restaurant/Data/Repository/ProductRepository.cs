public class ProductRepository : IProductRepository {
    private readonly RestaurantContext _context;

    public ProductRepository(RestaurantContext context) {
        _context = context;
    }

    // Haal producten op gebaseerd op CategorieType
    public async Task<IEnumerable<Product>> GetProductsByCategorieTypeAsync(int typeId) {
        return await _context.Producten
            .Include(p => p.Categorie)
            .Where(p => p.Categorie != null && p.Categorie.TypeId == typeId)
            .ToListAsync();
    }


    public async Task<Product> GetByIdAsync(int id) {
        return await _context.Producten
            .Include(p => p.Categorie)
            .Include(p => p.PrijsProducten)
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task AddAsync(Product product) {
        await _context.Producten.AddAsync(product);
    }

    public async Task UpdateAsync(Product product) {
        _context.Producten.Update(product);
    }

    public async Task DeleteAsync(int id) {
        var product = await _context.Producten.FindAsync(id);
        if (product != null) {
            _context.Producten.Remove(product);
        }
    }

    public async Task AddPrijsAsync(PrijsProduct prijs)
    {
        await _context.PrijsProducten.AddAsync(prijs);
    }
    public async Task DeletePrijsAsync(int prijsId)
    {
        var prijs = await _context.PrijsProducten.FindAsync(prijsId);
        if (prijs != null)
        {
            _context.PrijsProducten.Remove(prijs);
        }
    }
    public async Task<Product> GetProductWithPricesAsync(int id)
    {
        return await _context.Producten
            .Include(p => p.PrijsProducten)
            .FirstOrDefaultAsync(p => p.Id == id);
    }


    public async Task<IEnumerable<Categorie>> GetAllCategoriesAsync()
    {
        return await _context.Categorien
            .AsNoTracking()
            .OrderBy(c => c.Naam)
            .ToListAsync();
    }

    public async Task<IEnumerable<Categorie>> GetCategoriesByTypeAsync(int typeId)
    {
        return await _context.Categorien
            .Where(c => c.TypeId == typeId)
            .AsNoTracking()
            .OrderBy(c => c.Naam)
            .ToListAsync();
    }
}
