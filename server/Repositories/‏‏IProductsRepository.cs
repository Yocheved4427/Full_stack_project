using Entities;

namespace Repositories
{
    public interface IProductsRepository
    {
        public Task<(List<Product> Items, int TotalCount)> GetProducts(int position, int skip, int?[] categoryIds,
         string? description, int? maxPrice, int? minPrice);

    }
}