using DTOs;
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
                await _context.Orders.AddAsync(order);
                await _context.SaveChangesAsync();

                var reloadedOrder = await _context.Orders
                    .Include(o => o.OrderItems)
                        .ThenInclude(oi => oi.Product)
                            .ThenInclude(p => p.Images)
                    .FirstAsync(o => o.OrderId == order.OrderId);

                return reloadedOrder;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[OrdersRepository] ERROR: {ex.Message}");
                if (ex.InnerException != null)
                    Console.WriteLine($"[OrdersRepository] Inner: {ex.InnerException.Message}");
                throw;
            }
        }

        public async Task<List<Order>> GetOrdersByUserId(int userId)
        {
            var orders = await _context.Orders
                .Include(o => o.OrderItems)
                    .ThenInclude(oi => oi.Product)
                        .ThenInclude(p => p.Images)
                .Where(o => o.UserId == userId)
                .OrderByDescending(o => o.OrderDate)
                .ToListAsync();

            var today = DateOnly.FromDateTime(DateTime.Today);
            var hasChanges = false;

            foreach (var order in orders)
            {
                var hasStarted = order.OrderItems.Any(oi =>
                    oi.DepartureDate.HasValue && oi.DepartureDate.Value <= today);
                var allFinished = order.OrderItems.Any() &&
                    order.OrderItems.All(oi => oi.ReturnDate.HasValue && oi.ReturnDate.Value < today);

                var newStatus = allFinished ? "Completed"
                    : hasStarted ? "In Vacation"
                    : "waiting...";

                if (!string.Equals(order.Status, newStatus, StringComparison.OrdinalIgnoreCase))
                {
                    order.Status = newStatus;
                    hasChanges = true;
                }
            }

            if (hasChanges)
                await _context.SaveChangesAsync();

            return orders;
        }

        public async Task<List<Order>> GetAllOrders()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .OrderBy(o => o.OrderId)
                .ToListAsync();
        }

        public async Task<List<Order>> GetAllOrdersForAdmin()
        {
            return await _context.Orders
                .AsNoTracking()
                .Include(o => o.User)
                .Include(o => o.OrderItems)
                .OrderBy(o => o.OrderId)
                .ToListAsync();
        }

        public async Task<DashboardStatsDTO> GetDashboardStats()
        {
            var raw = await _context.Orders
                .AsNoTracking()
                .Select(o => new { o.UserId, o.OrderDate, o.OrderSum, o.Status })
                .ToListAsync();

            var totalOrders = raw.Count;
            var totalRevenue = raw.Sum(o => o.OrderSum ?? 0);
            var totalCustomers = raw.Select(o => o.UserId).Distinct().Count();
            var pendingOrders = raw.Count(o =>
                (o.Status ?? "").IndexOf("wait", StringComparison.OrdinalIgnoreCase) >= 0);

            var salesByMonth = raw
                .Where(o => o.OrderDate.HasValue)
                .GroupBy(o => o.OrderDate!.Value.Month)
                .Select(g => new MonthStatDTO
                {
                    Month = g.Key,
                    Revenue = g.Sum(o => o.OrderSum ?? 0),
                    OrderCount = g.Count()
                })
                .ToList();

            var ordersByStatus = raw
                .GroupBy(o => o.Status ?? "Unknown")
                .Select(g => new StatusStatDTO { Status = g.Key, Count = g.Count() })
                .ToList();

            return new DashboardStatsDTO
            {
                TotalOrders = totalOrders,
                TotalRevenue = totalRevenue,
                TotalCustomers = totalCustomers,
                PendingOrders = pendingOrders,
                SalesByMonth = salesByMonth,
                OrdersByStatus = ordersByStatus
            };
        }
    }
}
