using Microsoft.AspNetCore.Mvc;
using Services;
using Entities;
using DTOs;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ProductsController : ControllerBase
    {
        private readonly IProductsServices _IProductsServices;
        public ProductsController(IProductsServices productsServices)
        {
            _IProductsServices = productsServices;
        }
       
        [HttpGet]
        public async Task<ActionResult<PageResponseDTO<ProductDTO>>> Get(int position, int skip, [FromQuery] int?[] categoryIds, string? description, int? maxPrice, int? minPrice)
        {
            
            PageResponseDTO<ProductDTO> pageResponse = await _IProductsServices.GetProducts(position, skip, categoryIds, description, maxPrice, minPrice);
            return Ok(pageResponse);
        }



        //[HttpGet("{id}")]

        //[HttpPost("Login")]


        //[HttpPost]

        //// PUT api/<Users>/5
        //[HttpPut("{id}")]







    }
}


