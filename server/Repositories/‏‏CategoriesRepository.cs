using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using System.Text.Json;
namespace Repositories


{

    public class CategoriesRepository : ICategoriesRepository
    {
        public readonly ApiShopContext _context;
        public CategoriesRepository(ApiShopContext context)
        {
            _context = context;
        }
       public async Task<IEnumerable<Category>> GetCategories()
        {
            return await _context.Categories.ToListAsync();
        }

        public async Task<Category> GetCategoryById(int id)
        {
            return await _context.Categories.FindAsync(id);
        }

        public async Task<Category> AddCategory(Category category)
        {
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return category;
        }

        public async Task<Category> UpdateCategory(int id, Category category)
        {
            var existingCategory = await _context.Categories.FindAsync(id);
            if (existingCategory != null)
            {
                existingCategory.CategoryName = category.CategoryName;
                await _context.SaveChangesAsync();
            }
            return existingCategory;
        }

        public async Task DeleteCategory(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category != null)
            {
                _context.Categories.Remove(category);
                await _context.SaveChangesAsync();
            }
        }

    }
}
