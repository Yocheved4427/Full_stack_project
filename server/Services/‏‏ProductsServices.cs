using Entities;
using Repositories;
using DTOs;
using AutoMapper;
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
                product.Images?.Select(i => i.Url).ToList() ?? new List<string>(),
                product.ProductMonthConfigs?.Select(mc => new ProductMonthConfigDTO(
                    mc.ConfigId,
                    mc.MonthNumber ?? 0,
                    mc.IsAvailable,
                    mc.SpecialPrice ?? 0
                )).ToList() ?? new List<ProductMonthConfigDTO>()
            );
        }
    }
    }
