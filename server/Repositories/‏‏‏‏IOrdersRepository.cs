using Entities;

namespace Repositories
{
    public interface IOrdersRepository
    {
        Task<Order?> GetOrderById(int id);
        Task<Order> AddOrder(Order order);
    }
}