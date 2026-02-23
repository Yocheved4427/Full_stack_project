using DTOs;

namespace Services
{
    public interface ICategoriesServices
    {
        Task<IEnumerable<CategoryDTO>> GetCategories();
        Task<CategoryDTO> GetCategoryById(int id);
        Task<CategoryDTO> AddCategory(CategoryDTO categoryDto);
        Task<CategoryDTO> UpdateCategory(int id, CategoryDTO categoryDto);
        Task DeleteCategory(int id);
    }
}