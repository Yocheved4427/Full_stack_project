using Microsoft.AspNetCore.Mvc;
using Services;
using Entities;
using AutoMapper;
using DTOs;

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

        [HttpGet]
        public async Task<ActionResult<List<OrderDTO>>> GetAll()
        {
            var orders = await _ordersServices.GetAllOrders();
            return Ok(orders);
        }

        [HttpGet("dashboard-stats")]
        public async Task<ActionResult<DashboardStatsDTO>> GetDashboardStats()
        {
            var stats = await _ordersServices.GetDashboardStats();
            return Ok(stats);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO newOrder)
        {
            Console.WriteLine($"=== ORDER CREATION REQUEST ===");

            try
            {
                if (newOrder.UserId <= 0)
                    return BadRequest(new { message = "Valid userId is required" });

                newOrder = await _ordersServices.AddOrder(newOrder);

                if (newOrder == null)
                    return BadRequest("Failed to create order");

                return CreatedAtAction(nameof(Get), new { id = newOrder.OrderId }, newOrder);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERROR creating order: {ex.Message}");
                return StatusCode(500, new { error = ex.Message, innerError = ex.InnerException?.Message });
            }
        }
    }
}
