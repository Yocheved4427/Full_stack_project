using DTOs;
// שימי לב: הסרנו את using Entities ו-using Microsoft.EntityFrameworkCore
// כי הקונטרולר לא אמור להכיר אותם בכלל!
using Microsoft.AspNetCore.Mvc;
using Services;

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

        [HttpGet("health")]
        public IActionResult Health()
        {
            return Ok(new { status = "API is running", timestamp = DateTime.Now });
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<ProductDTO>> GetById(int id)
        {
            var product = await _IProductsServices.GetProductById(id);

            if (product == null)
                return NotFound();

            return Ok(product);
        }

        [HttpPost]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<ActionResult<ProductDTO>> AddProduct([FromBody] ProductDTO productDTO)
        {
            if (productDTO == null)
                return BadRequest("Product data is required");

            var createdProduct = await _IProductsServices.AddProduct(productDTO);
            
            if (createdProduct == null)
                return BadRequest("Failed to create product");

            return CreatedAtAction(nameof(GetById), new { id = createdProduct.ProductId }, createdProduct);
        }

        [HttpPut("{id}")]
        [RequestSizeLimit(104857600)] // 100 MB
        public async Task<ActionResult<ProductDTO>> UpdateProduct(int id, [FromBody] ProductDTO productDTO)
        {
            if (productDTO == null || id != productDTO.ProductId)
                return BadRequest("Invalid product data");

            var updatedProduct = await _IProductsServices.UpdateProduct(id, productDTO);

            if (updatedProduct == null)
                return NotFound();

            return Ok(updatedProduct);
        }

        // 👇 פונקציית המחיקה המתוקנת
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteProduct(int id)
        {
            // הקונטרולר רק מעביר את הבקשה לסרוויס ומקבל תשובה (כן/לא)
            bool isDeleted = await _IProductsServices.DeleteProduct(id);

            // אם הסרוויס החזיר false, סימן שהמוצר לא נמצא
            if (!isDeleted)
            {
                return NotFound();
            }

            // אם הכל עבר בהצלחה, מחזירים 204
            return NoContent();
        }
    }
}