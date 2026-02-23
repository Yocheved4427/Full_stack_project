
using Entities;
using Repositories;
using DTOs;
using AutoMapper;
namespace Services
{
    public class CategoriesServices : ICategoriesServices
    {
        private readonly ICategoriesRepository _categoriesRepository;
        private readonly IMapper _mapper;
        public CategoriesServices(ICategoriesRepository categoriesRepository, IMapper mapper)
        {
            _categoriesRepository = categoriesRepository;
            _mapper = mapper;
        }
        public async Task<IEnumerable<CategoryDTO>>GetCategories()
        {
            return _mapper.Map<IEnumerable<Category>,IEnumerable<CategoryDTO>>(await _categoriesRepository.GetCategories());
        }

        public async Task<CategoryDTO> GetCategoryById(int id)
        {
            var category = await _categoriesRepository.GetCategoryById(id);
            return _mapper.Map<Category, CategoryDTO>(category);
        }

        public async Task<CategoryDTO> AddCategory(CategoryDTO categoryDto)
        {
            var category = _mapper.Map<CategoryDTO, Category>(categoryDto);
            var addedCategory = await _categoriesRepository.AddCategory(category);
            return _mapper.Map<Category, CategoryDTO>(addedCategory);
        }

        public async Task<CategoryDTO> UpdateCategory(int id, CategoryDTO categoryDto)
        {
            var category = _mapper.Map<CategoryDTO, Category>(categoryDto);
            var updatedCategory = await _categoriesRepository.UpdateCategory(id, category);
            return _mapper.Map<Category, CategoryDTO>(updatedCategory);
        }

        public async Task DeleteCategory(int id)
        {
            await _categoriesRepository.DeleteCategory(id);
        }
       
    }
}
