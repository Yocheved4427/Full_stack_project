using Microsoft.AspNetCore.Mvc;
using Services;
using Entities;
using AutoMapper;
using DTOs;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersServices _ordersServices;
        public OrdersController(IOrdersServices ordersServices, IMapper mapper)
        {
            _ordersServices = ordersServices;
        }
       
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> Get(int id)
        {
            OrderDTO? order = await _ordersServices.GetOrderById(id);
            if (order != null)
                return Ok(order);
            return NotFound();
        }

        [HttpGet("user/{userId}")]
        public async Task<ActionResult<List<OrderDTO>>> GetOrdersByUserId(int userId)
        {
            var orders = await _ordersServices.GetOrdersByUserId(userId);
            return Ok(orders);
        }

        [HttpPost]
       public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO newOrder)
        {
            Console.WriteLine($"=== ORDER CREATION REQUEST ===");
            Console.WriteLine($"Raw request received");
            
            try
            {
                if (newOrder.UserId <= 0)
                {
                    return BadRequest(new { message = "Valid userId is required" });
                }

                Console.WriteLine($"User ID: {newOrder.UserId}");
                Console.WriteLine($"Order Date: {newOrder.OrderDate}");
                Console.WriteLine($"Order Sum: {newOrder.OrderSum}");
                Console.WriteLine($"Status: {newOrder.Status}");
                Console.WriteLine($"Order Items: {newOrder.OrderItems?.Count ?? 0}");
                
                if (newOrder.OrderItems != null)
                {
                    foreach (var item in newOrder.OrderItems)
                    {
                        Console.WriteLine($"  - Product {item.ProductId}: {item.ProductName}, Qty: {item.Quantity}");
                    }
                }
                
                newOrder = await _ordersServices.AddOrder(newOrder);
                
                if (newOrder == null)
                {
                    Console.WriteLine("ERROR: Order creation returned null");
                    return BadRequest("Failed to create order");
                }
                
                Console.WriteLine($"✅ Order created successfully with ID: {newOrder.OrderId}");
                return CreatedAtAction(nameof(Get), new { id = newOrder.OrderId }, newOrder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ ERROR creating order: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner exception: {ex.InnerException.Message}");
                }
                return StatusCode(500, new { error = ex.Message, innerError = ex.InnerException?.Message, stackTrace = ex.StackTrace });
            }
        }

    }
}


