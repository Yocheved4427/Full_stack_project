using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using System.Text.Json;
namespace Repositories


{

    public class ProductsRepository : IProductsRepository
    {

        public readonly ApiShopContext _context;
        public ProductsRepository(ApiShopContext context)
        {
            _context = context;
        }
        public async Task<(List<Product> Items, int TotalCount)> GetProducts(int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice)
        {
            
            var query = _context.Products.Where(product =>
                (string.IsNullOrEmpty(description) || 
                 EF.Functions.Like(product.Description, $"%{description}%") || 
                 EF.Functions.Like(product.ProductName, $"%{description}%")) &&
                (maxPrice == null || product.Price <= maxPrice) &&
                (minPrice == null || product.Price >= minPrice) &&
                (categoryIds == null || categoryIds.Length == 0 || categoryIds.Contains(product.CategoryId)))
                .OrderBy(product => product.Price);
            
            // Fix pagination: position starts from 1, so we do (position - 1) * skip
            var skipCount = (position - 1) * skip;
            if (skipCount < 0) skipCount = 0;
            
            List<Product> products = await query
                .AsNoTracking()
                .Skip(skipCount)
                .Take(skip)
                .Include(product => product.Category)
                .Include(product => product.Images)
                .ToListAsync();
            
            var total = await query.CountAsync();
            
            return (products, total);
        }

    }
}