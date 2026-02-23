using DTOs;
using AutoMapper;
using Repositories;
using Entities;
namespace Services
{
    public class OrdersServices : IOrdersServices
    {
        private readonly IOrdersRepository _orders;
        private readonly IMapper _mapper;
        public OrdersServices(IOrdersRepository orders,IMapper mapper)
        {
            _orders = orders;
            _mapper = mapper;

        }
        public async Task<OrderDTO?> GetOrderById(int id)
        {
            var order = await _orders.GetOrderById(id);
            if(order == null)
            {
                return null;
            }
            return _mapper.Map<Order, OrderDTO>(order);
        }

        public async Task<OrderDTO> AddOrder(OrderDTO order)
        {
            try
            {
                Console.WriteLine("[OrdersServices] Starting to add order");
                Console.WriteLine($"[OrdersServices] Mapping DTO to Entity - OrderItems count: {order.OrderItems?.Count ?? 0}");
                
                var orderEntity = _mapper.Map<OrderDTO, Order>(order);
                
                Console.WriteLine($"[OrdersServices] Mapped successfully - OrderItems count: {orderEntity.OrderItems?.Count ?? 0}");
                Console.WriteLine($"[OrdersServices] UserId: {orderEntity.UserId}, OrderDate: {orderEntity.OrderDate}, OrderSum: {orderEntity.OrderSum}");
                
                var savedOrder = await _orders.AddOrder(orderEntity);
                
                Console.WriteLine($"[OrdersServices] Order added to repository with ID: {savedOrder.OrderId}");
                
                var result = _mapper.Map<Order, OrderDTO>(savedOrder);
                
                Console.WriteLine($"[OrdersServices] Mapped back to DTO - OrderId: {result.OrderId}");
                
                return result;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrdersServices] ERROR: {ex.Message}");
                Console.WriteLine($"[OrdersServices] Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[OrdersServices] Inner: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<List<OrderDTO>> GetOrdersByUserId(int userId)
        {
            var orders = await _orders.GetOrdersByUserId(userId);
            return _mapper.Map<List<Order>, List<OrderDTO>>(orders);
        }
    }
}
