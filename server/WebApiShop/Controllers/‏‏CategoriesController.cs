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


        //[HttpGet("{id}")]

        //[HttpPost("Login")]


        //[HttpPost]


        //// PUT api/<Users>/5
        //[HttpPut("{id}")]

    }
}


