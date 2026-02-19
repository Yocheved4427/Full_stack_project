
namespace DTOs
{
    public record OrderDTO
    (
        int? OrderId,

        int UserId,

        DateOnly OrderDate ,

        double OrderSum,

        string Status,

        ICollection<OrderItemDTO> OrderItems
    );
}

  
