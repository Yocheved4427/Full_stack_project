using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Repositories;

public class OrderStatusUpdaterService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;

    public OrderStatusUpdaterService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<ApiShopContext>();
                var today = DateOnly.FromDateTime(DateTime.Today);

                // שליפת כל ההזמנות שעדיין לא הסתיימו ("Completed")
                var activeOrders = await context.Orders
                    .Include(o => o.OrderItems) // חשוב מאוד כדי לגשת לתאריכים בפריטים
                    .Where(o => o.Status != "Completed")
                    .ToListAsync();

                foreach (var order in activeOrders)
                {
                    // בדיקה האם יש לפחות פריט אחד שהתאריך שלו התחיל
                    bool hasStarted = order.OrderItems.Any(oi => oi.DepartureDate <= today);
                    // בדיקה האם כל הפריטים כבר הסתיימו
                    bool allFinished = order.OrderItems.All(oi => oi.ReturnDate < today);

                    if (allFinished)
                    {
                        order.Status = "Completed";
                    }
                    else if (hasStarted)
                    {
                        order.Status = "In Vacation";
                    }
                    else
                    {
                        order.Status = "waiting...";
                    }
                }

                await context.SaveChangesAsync();
            }
            await Task.Delay(TimeSpan.FromHours(24), stoppingToken);
        }
    }

}