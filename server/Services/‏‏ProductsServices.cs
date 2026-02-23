using AutoMapper;
using DTOs;
using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
namespace Services
{
    public class ProductsServices : IProductsServices
    {
        private readonly IProductsRepository _repository;
        private readonly IMapper _mapper;
        public ProductsServices(IProductsRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }
        public async Task<PageResponseDTO<ProductDTO>> GetProducts(int position, int skip, int?[] categoryIds, string? description, int? maxPrice, int? minPrice)
        {
            // --- הוספת ברירות מחדל ---
            // אם לא שלחו עמוד, נתחיל מעמוד 1
            if (position <= 0) position = 1;

            // אם לא שלחו כמות (או שלחו 0), נחזיר 10 מוצרים כברירת מחדל (או כל מספר שתרצי)
            if (skip <= 0) skip = 10;
            // -------------------------

            // 1. שליפת הנתונים (עם הערכים המתוקנים)
            var response = await _repository.GetProducts(position, skip, categoryIds, description, maxPrice, minPrice);

            // 2. המרה ידנית (כמו שעשינו קודם כדי לעקוף את AutoMapper)
            List<ProductDTO> data = response.Item1.Select(p => new ProductDTO(
                p.ProductId,
                p.ProductName ?? "",
                p.Description ?? "",
                p.CategoryId ?? 0,
                (decimal)(p.Price ?? 0),
                p.Images?.FirstOrDefault(i => i.IsMain)?.Url ?? "",
                p.IsActive,
                p.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                p.ProductMonthConfigs?.Select(mc => new ProductMonthConfigDTO(
                    mc.ConfigId,
                    mc.MonthNumber ?? 0,
                    mc.IsAvailable,
                    mc.SpecialPrice ?? 0
                )).ToList() ?? new List<ProductMonthConfigDTO>()
            )).ToList();

            // 3. יצירת התשובה
            PageResponseDTO<ProductDTO> pageResponse = new()
            {
                Data = data,
                TotalItems = response.Item2,
                CurrentPage = position,
                PageSize = skip,
                HasPreviousPage = position > 1
            };

            // חישוב עמודים
            int totalPages = (int)Math.Ceiling((double)pageResponse.TotalItems / skip);
            pageResponse.HasNextPage = position < totalPages;

            return pageResponse;
        }

        public async Task<ProductDTO?> GetProductById(int productId)
        {
            var product = await _repository.GetProductById(productId);
            
            if (product == null)
                return null;

            return new ProductDTO(
                product.ProductId,
                product.ProductName ?? "",
                product.Description ?? "",
                product.CategoryId ?? 0,
                (decimal)(product.Price ?? 0),
                product.Images?.FirstOrDefault(i => i.IsMain)?.Url ?? "",
                product.IsActive,
                product.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                product.ProductMonthConfigs?.Select(mc => new ProductMonthConfigDTO(
                    mc.ConfigId,
                    mc.MonthNumber ?? 0,
                    mc.IsAvailable,
                    mc.SpecialPrice ?? 0
                )).ToList() ?? new List<ProductMonthConfigDTO>()
            );
        }
        // הקוד הזה צריך להיות בתוך ProductsServices.cs
        public async Task<ProductDTO?> AddProduct(ProductDTO productDTO)
        {
            // Create new Product entity from DTO
            var product = new Product
            {
                ProductName = productDTO.ProductName,
                Description = productDTO.Description,
                CategoryId = productDTO.CategoryId,
                Price = productDTO.Price,
                IsActive = true, // New products are active by default
                Images = productDTO.ImageUrls?.Select((url, index) => new Entities.Image
                {
                    Url = url,
                    IsMain = url == productDTO.MainImageUrl || (index == 0 && string.IsNullOrEmpty(productDTO.MainImageUrl))
                }).ToList() ?? new List<Entities.Image>(),
                ProductMonthConfigs = productDTO.MonthConfigs?.Select(mc => new ProductMonthConfig
                {
                    MonthNumber = mc.MonthNumber,
                    IsAvailable = mc.IsAvailable,
                    SpecialPrice = mc.SpecialPrice
                }).ToList() ?? new List<ProductMonthConfig>()
            };

            var createdProduct = await _repository.AddProduct(product);

            if (createdProduct == null)
                return null;

            // Return as DTO
            return new ProductDTO(
                createdProduct.ProductId,
                createdProduct.ProductName ?? "",
                createdProduct.Description ?? "",
                createdProduct.CategoryId ?? 0,
                (decimal)(createdProduct.Price ?? 0),
                createdProduct.Images?.FirstOrDefault(i => i.IsMain)?.Url ?? "",
                createdProduct.IsActive,
                createdProduct.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                createdProduct.ProductMonthConfigs?.Select(mc => new ProductMonthConfigDTO(
                    mc.ConfigId,
                    mc.MonthNumber ?? 0,
                    mc.IsAvailable,
                    mc.SpecialPrice ?? 0
                )).ToList() ?? new List<ProductMonthConfigDTO>()
            );
        }

        public async Task<ProductDTO?> UpdateProduct(int id, ProductDTO productDTO)
        {
            var existingProduct = await _repository.GetProductById(id);

            if (existingProduct == null)
                return null;

            // Update product properties
            existingProduct.ProductName = productDTO.ProductName;
            existingProduct.Description = productDTO.Description;
            existingProduct.CategoryId = productDTO.CategoryId;
            existingProduct.Price = productDTO.Price;
            existingProduct.IsActive = productDTO.IsActive;

            // Clear existing images and add new ones
            existingProduct.Images?.Clear();
            existingProduct.Images = productDTO.ImageUrls?.Select((url, index) => new Entities.Image
            {
                ProductId = id,
                Url = url,
                IsMain = url == productDTO.MainImageUrl
            }).ToList() ?? new List<Entities.Image>();

            // Clear existing month configs and add new ones
            existingProduct.ProductMonthConfigs?.Clear();
            existingProduct.ProductMonthConfigs = productDTO.MonthConfigs?.Select(mc => new ProductMonthConfig
            {
                ProductId = id,
                MonthNumber = mc.MonthNumber,
                IsAvailable = mc.IsAvailable,
                SpecialPrice = mc.SpecialPrice
            }).ToList() ?? new List<ProductMonthConfig>();

            await _repository.UpdateProduct(existingProduct);

            // Return updated product as DTO
            return new ProductDTO(
                existingProduct.ProductId,
                existingProduct.ProductName ?? "",
                existingProduct.Description ?? "",
                existingProduct.CategoryId ?? 0,
                (decimal)(existingProduct.Price ?? 0),
                existingProduct.Images?.FirstOrDefault(i => i.IsMain)?.Url ?? "",
                existingProduct.IsActive,
                existingProduct.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                existingProduct.ProductMonthConfigs?.Select(mc => new ProductMonthConfigDTO(
                    mc.ConfigId,
                    mc.MonthNumber ?? 0,
                    mc.IsAvailable,
                    mc.SpecialPrice ?? 0
                )).ToList() ?? new List<ProductMonthConfigDTO>()
            );
        }

        public async Task<bool> DeleteProduct(int id)
        {
            var product = await _repository.GetProductById(id);

            if (product == null)
            {
                return false;
            }

            product.IsActive = false; // המחיקה הרכה
            await _repository.UpdateProduct(product); // שמירה דרך הרפוזיטורי

            return true;
        }
    }
    }
