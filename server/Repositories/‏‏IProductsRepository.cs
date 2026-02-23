using Entities;

namespace Repositories
{
    public interface IProductsRepository
    {
        public Task<(List<Product> Items, int TotalCount)> GetProducts(int position, int skip, int?[] categoryIds,
         string? description, int? maxPrice, int? minPrice,bool includeInactive = false);

        public Task<Product?> GetProductById(int productId);

        public Task<Product> AddProduct(Product product);

        public Task UpdateProduct(Product product);


    }
}