
namespace DTOs
{
    public record OrderItemDTO(
    int ProductId,
    string ProductName,
    string ImageUrl,
    int Quantity,
    DateOnly DepartureDate,
    DateOnly ReturnDate,
    int NightsCount,
    decimal PricePerUnit
);

}
