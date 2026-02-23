using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        public readonly ApiShopContext _context;

        public ProductsRepository(ApiShopContext context)
        {
            _context = context;
        }

        // 👇 התוספת כאן: includeInactive = false
        public async Task<(List<Product> Items, int TotalCount)> GetProducts(int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice, bool includeInactive = false)
        {
            var query = _context.Products.Where(product =>
                // הסינון החדש: אם זה לא מנהל שמבקש הכל, נביא רק את הפעילים
                (includeInactive || product.IsActive == true) &&
                (string.IsNullOrEmpty(description) ||
                 EF.Functions.Like(product.Description, $"%{description}%") ||
                 EF.Functions.Like(product.ProductName, $"%{description}%")) &&
                (maxPrice == null || product.Price <= maxPrice) &&
                (minPrice == null || product.Price >= minPrice) &&
                (categoryIds == null || categoryIds.Length == 0 || categoryIds.Contains(product.CategoryId)))
                .OrderBy(product => product.Price);

            var skipCount = (position - 1) * skip;
            if (skipCount < 0) skipCount = 0;

            List<Product> products = await query
                .AsNoTracking()
                .Skip(skipCount)
                .Take(skip)
                .Include(product => product.Category)
                .Include(product => product.Images)
                .Include(product => product.ProductMonthConfigs)
                .ToListAsync();

            var total = await query.CountAsync();

            return (products, total);
        }

        public async Task<Product?> GetProductById(int productId)
        {
            // כאן השארתי את זה בלי סינון IsActive, כדי שאם לקוח נכנס להזמנה ישנה,
            // הוא עדיין יוכל לראות את פרטי המוצר שהוא קנה בעבר, גם אם המוצר כבר לא פעיל בחנות.
            return await _context.Products
                .AsNoTracking()
                .Include(product => product.Category)
                .Include(product => product.Images)
                .Include(product => product.ProductMonthConfigs)
                .FirstOrDefaultAsync(p => p.ProductId == productId);
        }

        // ==========================================
        // 👇 פונקציות חדשות עבור אזור הניהול (Admin)
        // ==========================================

        public async Task<Product> AddProduct(Product product)
        {
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product; // מחזירים את המוצר כדי לקבל את ה-ID שנוצר לו במסד הנתונים
        }

        public async Task UpdateProduct(Product product)
        {
            // Get the existing product WITH tracking to properly handle related entities
            var existingProduct = await _context.Products
                .Include(p => p.Images)
                .Include(p => p.ProductMonthConfigs)
                .FirstOrDefaultAsync(p => p.ProductId == product.ProductId);

            if (existingProduct == null)
                throw new InvalidOperationException($"Product with ID {product.ProductId} not found");

            // Update scalar properties
            existingProduct.ProductName = product.ProductName;
            existingProduct.Description = product.Description;
            existingProduct.CategoryId = product.CategoryId;
            existingProduct.Price = product.Price;
            existingProduct.IsActive = product.IsActive;

            // Remove all existing images
            if (existingProduct.Images != null)
            {
                _context.Images.RemoveRange(existingProduct.Images);
            }
            else
            {
                existingProduct.Images = new List<Entities.Image>();
            }

            // Add new images
            if (product.Images != null && product.Images.Any())
            {
                foreach (var image in product.Images)
                {
                    existingProduct.Images.Add(image);
                }
            }

            // Remove all existing month configs
            if (existingProduct.ProductMonthConfigs != null)
            {
                _context.ProductMonthConfigs.RemoveRange(existingProduct.ProductMonthConfigs);
            }
            else
            {
                existingProduct.ProductMonthConfigs = new List<ProductMonthConfig>();
            }

            // Add new month configs
            if (product.ProductMonthConfigs != null && product.ProductMonthConfigs.Any())
            {
                foreach (var config in product.ProductMonthConfigs)
                {
                    existingProduct.ProductMonthConfigs.Add(config);
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}