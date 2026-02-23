
namespace DTOs
{
    public record OrderItemDTO(
    int ProductId,
    string ProductName,
    string ImageUrl,
    int Quantity,
    string DepartureDate,
    string ReturnDate,
    int NightsCount,
    decimal PricePerUnit
)
    {
        public OrderItemDTO() : this(0, string.Empty, string.Empty, 0, string.Empty, string.Empty, 0, 0m)
        {
        }
    }

}
