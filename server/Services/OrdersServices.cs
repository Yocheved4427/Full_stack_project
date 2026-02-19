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
            return _mapper.Map<Order, OrderDTO>(await _orders.AddOrder(_mapper.Map<OrderDTO, Order>(order)));
        }

      
    }
}
