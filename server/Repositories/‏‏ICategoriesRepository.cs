using Entities;

namespace Repositories
{
    public interface ICategoriesRepository
    {
        Task<IEnumerable<Category>> GetCategories();

    }
}