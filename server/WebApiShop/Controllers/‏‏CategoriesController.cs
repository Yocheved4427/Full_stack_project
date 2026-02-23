using Microsoft.AspNetCore.Mvc;
using Services;
using Entities;
using DTOs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CategoriesController : ControllerBase
    {
        private readonly ICategoriesServices _ICategoriesServices;
        public CategoriesController(ICategoriesServices categoriesServices)
        {
            _ICategoriesServices = categoriesServices;
        }
        


        [HttpGet]
        public async Task<ActionResult<IEnumerable<CategoryDTO>>> Get()
        {
            IEnumerable<CategoryDTO> categories = await _ICategoriesServices.GetCategories();
            if (categories != null && categories.Any())
                return Ok(categories);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<CategoryDTO>> GetById(int id)
        {
            var category = await _ICategoriesServices.GetCategoryById(id);
            if (category != null)
                return Ok(category);
            return NotFound();
        }

        [HttpPost]
        public async Task<ActionResult<CategoryDTO>> Post([FromBody] CategoryDTO categoryDto)
        {
            try
            {
                var addedCategory = await _ICategoriesServices.AddCategory(categoryDto);
                return CreatedAtAction(nameof(GetById), new { id = addedCategory.CategoryId }, addedCategory);
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public async Task<ActionResult<CategoryDTO>> Put(int id, [FromBody] CategoryDTO categoryDto)
        {
            try
            {
                var updatedCategory = await _ICategoriesServices.UpdateCategory(id, categoryDto);
                if (updatedCategory != null)
                    return Ok(updatedCategory);
                return NotFound();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public async Task<ActionResult> Delete(int id)
        {
            try
            {
                await _ICategoriesServices.DeleteCategory(id);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

    }
}


