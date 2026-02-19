using Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Xunit;

namespace TestProject
{
    public class OrderRepositoryUnitTests
    {
        [Fact]
        
        public async Task GetOrderById_ShouldReturnOrder_WhenOrderExists()
        {
            var options = new DbContextOptions<ApiShopContext>();
            var mockContext = new Mock<ApiShopContext>(options);
            var orderId = 10;
            var order = new Order { OrderId = orderId, OrderSum = 500, UserId = 1 };
      
            var mockSet = new Mock<DbSet<Order>>();
            mockSet.Setup(m => m.FindAsync(orderId))
                   .Returns(new ValueTask<Order?>(order));
            mockContext.Setup(x => x.Orders).Returns(mockSet.Object);

            var repository = new OrdersRepository(mockContext.Object);
            var result = await repository.GetOrderById(orderId);
            Assert.NotNull(result);
            Assert.Equal(500, result.OrderSum);
            Assert.Equal(orderId, result.OrderId);
        }

        [Fact]
        public async Task GetOrderById_ShouldReturnNull_WhenOrderDoesNotExist()
        {
            var options = new DbContextOptions<ApiShopContext>();
            var mockContext = new Mock<ApiShopContext>(options);
     
            mockContext.Setup(x => x.Orders).ReturnsDbSet(new List<Order>());
            var repository = new OrdersRepository(mockContext.Object);
            var result = await repository.GetOrderById(1);
            Assert.Null(result);
        }

        [Fact]
        public async Task AddOrder_ShouldAddOrderAndSaveChanges()
        {
            var options = new DbContextOptions<ApiShopContext>();
            var mockContext = new Mock<ApiShopContext>(options);

            var mockSet = new Mock<DbSet<Order>>();
            mockContext.Setup(m => m.Orders).Returns(mockSet.Object);
            var repository = new OrdersRepository(mockContext.Object);
            var order = new Order { OrderSum = 100, OrderDate = DateOnly.FromDateTime(DateTime.Now) };
            var result = await repository.AddOrder(order);
            mockSet.Verify(m => m.AddAsync(order, default), Times.Once());
            mockContext.Verify(m => m.SaveChangesAsync(default), Times.Once());
            Assert.Equal(order, result);
        }
    }
}