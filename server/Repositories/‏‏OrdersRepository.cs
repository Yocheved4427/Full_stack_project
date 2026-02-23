using Entities;
using Microsoft.EntityFrameworkCore;

namespace Repositories


{

    public class OrdersRepository : IOrdersRepository
    {
       public readonly ApiShopContext _context;
        public OrdersRepository(ApiShopContext context)
        {
            _context = context;
        }
        public async Task<Order?> GetOrderById(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }
        public async Task<Order> AddOrder(Order order)
        {
            try
            {
                Console.WriteLine("[OrdersRepository] Starting to add order to database");
                Console.WriteLine($"[OrdersRepository] Order details - UserId: {order.UserId}, OrderSum: {order.OrderSum}, OrderItems: {order.OrderItems?.Count ?? 0}");
                
                await _context.Orders.AddAsync(order);
                Console.WriteLine("[OrdersRepository] Order added to context");
                
                var saveResult = await _context.SaveChangesAsync();
                Console.WriteLine($"[OrdersRepository] SaveChanges completed - affected rows: {saveResult}");
                Console.WriteLine($"[OrdersRepository] Order ID after save: {order.OrderId}");
                
                // Reload with Product navigation properties
                var reloadedOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Images)
                    .FirstAsync(o => o.OrderId == order.OrderId);
                
                Console.WriteLine($"[OrdersRepository] Order reloaded successfully with {reloadedOrder.OrderItems?.Count ?? 0} items");
                
                return reloadedOrder;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrdersRepository] ERROR: {ex.Message}");
                Console.WriteLine($"[OrdersRepository] Stack: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[OrdersRepository] Inner: {ex.InnerException.Message}");
                }
                throw;
            }
        }

        public async Task<List<Order>> GetOrdersByUserId(int userId)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();
        }

    }
}
