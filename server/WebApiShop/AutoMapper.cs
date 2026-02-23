using AutoMapper;
using DTOs;
using Entities;

namespace WebApiShop
{
    public class AutoMapper : Profile
    {
        public AutoMapper()
        {
            // Order mapping with date conversion
            CreateMap<OrderDTO, Order>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.OrderDate) ? DateOnly.Parse(src.OrderDate) : (DateOnly?)null))
                .ForMember(dest => dest.OrderId, opt => opt.Ignore())
                .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems))
                .ForMember(dest => dest.User, opt => opt.Ignore());
            
            CreateMap<Order, OrderDTO>()
                .ForMember(dest => dest.OrderDate, opt => opt.MapFrom(src => 
                    src.OrderDate.HasValue ? src.OrderDate.Value.ToString("yyyy-MM-dd") : ""))
                .ForMember(dest => dest.OrderItems, opt => opt.MapFrom(src => src.OrderItems));
            
            // Map OrderItemDTO to OrderItem (ignore fields that don't exist in entity)
            CreateMap<OrderItemDTO, OrderItem>()
                .ForMember(dest => dest.OrderItemId, opt => opt.Ignore())
                .ForMember(dest => dest.OrderId, opt => opt.Ignore())
                .ForMember(dest => dest.Order, opt => opt.Ignore())
                .ForMember(dest => dest.Product, opt => opt.Ignore())
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity))
                .ForMember(dest => dest.DepartureDate, opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.DepartureDate) ? DateOnly.Parse(src.DepartureDate) : (DateOnly?)null))
                .ForMember(dest => dest.ReturnDate, opt => opt.MapFrom(src => 
                    !string.IsNullOrEmpty(src.ReturnDate) ? DateOnly.Parse(src.ReturnDate) : (DateOnly?)null))
                .ForMember(dest => dest.NightsCount, opt => opt.MapFrom(src => src.NightsCount));
            
            // Map OrderItem to OrderItemDTO (populate additional fields from Product if available)
            CreateMap<OrderItem, OrderItemDTO>()
                .ForMember(dest => dest.ProductId, opt => opt.MapFrom(src => src.ProductId ?? 0))
                .ForMember(dest => dest.ProductName, opt => opt.MapFrom(src => src.Product != null ? src.Product.ProductName : ""))
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.Product != null && src.Product.Images.Any() ? src.Product.Images.First().Url : ""))
                .ForMember(dest => dest.Quantity, opt => opt.MapFrom(src => src.Quantity ?? 0))
                .ForMember(dest => dest.DepartureDate, opt => opt.MapFrom(src => src.DepartureDate.HasValue ? src.DepartureDate.Value.ToString("yyyy-MM-dd") : ""))
                .ForMember(dest => dest.ReturnDate, opt => opt.MapFrom(src => src.ReturnDate.HasValue ? src.ReturnDate.Value.ToString("yyyy-MM-dd") : ""))
                .ForMember(dest => dest.NightsCount, opt => opt.MapFrom(src => src.NightsCount ?? 0))
                .ForMember(dest => dest.PricePerUnit, opt => opt.MapFrom(src => src.Product != null ? src.Product.Price ?? 0 : 0));
            
            CreateMap<Category, CategoryDTO>().ReverseMap();
            
            // User mapping with explicit Id/UserId mapping
            CreateMap<User, UserDTO>()
                .ForMember(dest => dest.Id, opt => opt.MapFrom(src => src.UserId));
            CreateMap<UserDTO, User>()
                .ForMember(dest => dest.UserId, opt => opt.MapFrom(src => src.Id))
                .ForMember(dest => dest.Orders, opt => opt.Ignore());
            
            CreateMap<User, ExistingUserDTO>().ReverseMap();
            CreateMap<Product, ProductDTO>().ReverseMap();
            CreateMap<Image, ImageDTO>().ReverseMap();
            CreateMap<ProductMonthConfig, ProductMonthConfigDTO>().ReverseMap();

        }
    }
}