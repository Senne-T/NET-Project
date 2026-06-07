namespace Restaurant.Data.Repository
{
    public interface IProductRepository
    {
        Task<IEnumerable<Product>> GetProductsByCategorieTypeAsync(int typeId);
        Task<Product> GetByIdAsync(int id);
        Task AddAsync(Product product);
        Task UpdateAsync(Product product);
        Task DeleteAsync(int id);
        Task AddPrijsAsync(PrijsProduct prijs);
        Task DeletePrijsAsync(int prijsId);
        Task<Product> GetProductWithPricesAsync(int id);

        Task<IEnumerable<Categorie>> GetAllCategoriesAsync();
        Task<IEnumerable<Categorie>> GetCategoriesByTypeAsync(int typeId);
    }

}