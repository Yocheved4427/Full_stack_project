
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
       
    }
}
