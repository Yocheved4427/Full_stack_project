using Entities;
using DTOs;
namespace Services
{
    public interface IOrdersServices
    {
        Task<OrderDTO?> GetOrderById(int id);
        Task<OrderDTO> AddOrder(OrderDTO order);
    }
}