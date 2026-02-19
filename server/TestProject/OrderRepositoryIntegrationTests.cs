using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;
using System;
using System.Threading.Tasks;
using Xunit;

namespace TestProject
{
    public class OrderRepositoryIntegrationTests :IDisposable
    {
        private readonly DatabaseFixture _fixture;
        private readonly ApiShopContext _dbContext;
        private readonly OrdersRepository _ordersRepository;

        public OrderRepositoryIntegrationTests()
        {
            _fixture = new DatabaseFixture();
            _dbContext = _fixture.Context;
            _ordersRepository = new OrdersRepository(_dbContext);
        }
        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task AddOrder_ShouldAddOrderToDatabase_WhenOrderIsValid()
        {
            _dbContext.Orders.RemoveRange(_dbContext.Orders);
            _dbContext.Users.RemoveRange(_dbContext.Users);
            await _dbContext.SaveChangesAsync();
            var user = new User 
            { 
                Email = "order_test@gmail.com", 
                FirstName = "Test", 
                LastName = "Buyer", 
                Password = "123" 
            };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
           
            var orderToAdd = new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                OrderSum = 150.5,
                UserId = user.Id 
            };

            var result = await _ordersRepository.AddOrder(orderToAdd);

            Assert.NotNull(result);
            Assert.True(result.OrderId > 0); 
            Assert.Equal(150.5, result.OrderSum);

            _dbContext.ChangeTracker.Clear();
            var orderInDb = await _dbContext.Orders.FindAsync(result.OrderId);
            Assert.NotNull(orderInDb);
            Assert.Equal(user.Id, orderInDb.UserId);
        }

        [Fact]
        public async Task GetOrderById_ShouldReturnOrder_WhenOrderExists()
        {

            _dbContext.Orders.RemoveRange(_dbContext.Orders);
            _dbContext.Users.RemoveRange(_dbContext.Users);
            await _dbContext.SaveChangesAsync();
   
            var user = new User { Email = "fetch@test.com", FirstName = "A", LastName = "B", Password = "123" };
            await _dbContext.Users.AddAsync(user);
            await _dbContext.SaveChangesAsync();
            var order = new Order
            {
                OrderDate = new DateOnly(2025, 1, 1),
                OrderSum = 200,
                UserId = user.Id
            };
            await _dbContext.Orders.AddAsync(order);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear(); 
            var result = await _ordersRepository.GetOrderById(order.OrderId);
            Assert.NotNull(result);
            Assert.Equal(200, result.OrderSum);
            Assert.Equal(user.Id, result.UserId);
        }

        [Fact]
        public async Task GetOrderById_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            _dbContext.Orders.RemoveRange(_dbContext.Orders);
            await _dbContext.SaveChangesAsync();
            var result = await _ordersRepository.GetOrderById(9999);
            Assert.Null(result);
        }
    }
}