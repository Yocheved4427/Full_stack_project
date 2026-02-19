using DTOs;

namespace Services
{
    public interface ICategoriesServices
    {
        Task<IEnumerable<CategoryDTO>> GetCategories();
    }
}