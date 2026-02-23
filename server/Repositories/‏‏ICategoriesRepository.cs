using Entities;

namespace Repositories
{
    public interface ICategoriesRepository
    {
        Task<IEnumerable<Category>> GetCategories();
        Task<Category> GetCategoryById(int id);
        Task<Category> AddCategory(Category category);
        Task<Category> UpdateCategory(int id, Category category);
        Task DeleteCategory(int id);
    }
}