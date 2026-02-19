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

    }
}
