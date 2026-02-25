namespace DTOs
{
    public record OrderDTO
    (
        int? OrderId,
        int UserId,
        string? CustomerName,
        string OrderDate,
        double OrderSum,
        string Status,
        ICollection<OrderItemDTO> OrderItems
    )
    {
        public OrderDTO() : this(null, 0, null, string.Empty, 0, string.Empty, new List<OrderItemDTO>())
        {
        }
    }
}