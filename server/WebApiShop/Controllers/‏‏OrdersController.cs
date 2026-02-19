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

        [HttpPost]
       public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO newOrder)
        {
            newOrder = await _ordersServices.AddOrder(newOrder);
            if (newOrder == null)
                return BadRequest();
            return CreatedAtAction(nameof(Get), new { id = newOrder.OrderId }, newOrder);
        }






    }
}


