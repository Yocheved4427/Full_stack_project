using Entities;
using DTOs;
namespace Services
{
    public interface IProductsServices
    {
        public Task<PageResponseDTO<ProductDTO>> GetProducts(int position, int skip, int?[] categoryIds,
          string? description, int? maxPrice, int? minPrice);

        public Task<ProductDTO?> GetProductById(int productId);

        public Task<ProductDTO?> AddProduct(ProductDTO productDTO);

        public Task<ProductDTO?> UpdateProduct(int id, ProductDTO productDTO);

        public Task<bool> DeleteProduct(int id);
    }
}