using Entities;
using Microsoft.EntityFrameworkCore;
using Moq;
using Moq.EntityFrameworkCore;
using Repositories;
namespace TestProject
{
    public class UserRepositoryUnitTesting
    {
        [Fact]
        public async Task Register_ShouldAddUserToDatabase_WhenUserIsValid()
        {
            var mockContext = new Mock<ApiShopContext>(); 
            mockContext.Setup(x => x.Users).ReturnsDbSet(new List<User>());
            var repository = new UserRepository(mockContext.Object);
            var newUser = new User
            {
                Email = "new@test.com",
                FirstName = "New",
                LastName = "User",
                Password = "123"
            };
            var result = await repository.Register(newUser);
            Assert.NotNull(result);
            mockContext.Verify(x => x.Users.AddAsync(It.IsAny<User>(), default), Times.Once);
            mockContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }

        
        [Fact]
        public async Task GetUsers_ShouldReturnAllUsers()
        {
            var mockContext = new Mock<ApiShopContext>();
       
            var usersList = new List<User>
        {
            new User { UserId = 1, FirstName = "A", Email = "a@test.com" },
            new User { UserId = 2, FirstName = "B", Email = "b@test.com" }
        };
         
            mockContext.Setup(x => x.Users).ReturnsDbSet(usersList);
            var repository = new UserRepository(mockContext.Object);
            var result = await repository.GetUsers();
            Assert.NotNull(result);
            Assert.Equal(2, result.Count());
            Assert.Equal("A", result.First().FirstName);
        }
        [Fact]
        public async Task Update_ShouldModifyUserInDatabase()
        {
            var mockContext = new Mock<ApiShopContext>();
            mockContext.Setup(x => x.Users).ReturnsDbSet(new List<User>());
            var repository = new UserRepository(mockContext.Object);
            var userToUpdate = new User
            {
                UserId = 1,
                Email = "update@test.com",
                FirstName = "OldName",
                LastName = "User",
                Password = "123"
            };
            userToUpdate.FirstName = "NewName";
            await repository.Update(1, userToUpdate);
            mockContext.Verify(x => x.Users.Update(It.IsAny<User>()), Times.Once);
      
            mockContext.Verify(x => x.SaveChangesAsync(default), Times.Once);
        }
        [Fact]
        public async Task Login_ShouldReturnUser_WhenCredentialsAreCorrect()
        {
            var mockContext = new Mock<ApiShopContext>();
            var usersList = new List<User>
    {
        new User
        {
            UserId = 1,
            Email = "login@test.com",
            Password = "Password123",
            FirstName = "Login",
            Orders = new List<Order>
            {
                new Order
                {
                    OrderId = 100,
                    OrderDate = DateOnly.FromDateTime(DateTime.Now),
                    OrderSum = 250.5M
                }
            }
        }
    };

            mockContext.Setup(x => x.Users).ReturnsDbSet(usersList);

            var repository = new UserRepository(mockContext.Object);
            var result = await repository.Login("login@test.com", "Password123");
            Assert.NotNull(result);
            Assert.Equal("login@test.com", result.Email);
          
            Assert.NotEmpty(result.Orders);
            Assert.Equal(250.5M, result.Orders.First().OrderSum);
        }
        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenIdExists()
        {
            var mockContext = new Mock<ApiShopContext>();
            var mockSet = new Mock<DbSet<User>>();

            var user = new User
            {
                UserId = 1,
                Email = "find@test.com",
                FirstName = "Find",
                LastName = "Me",
                Password = "123"
            };

            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>(), It.IsAny<CancellationToken>()))
                   .Returns(new ValueTask<User?>(user));

            mockSet.Setup(m => m.FindAsync(It.IsAny<object[]>()))
                   .Returns(new ValueTask<User?>(user));
    
            mockContext.Setup(x => x.Users).Returns(mockSet.Object);

            var repository = new UserRepository(mockContext.Object);
            var result = await repository.GetUserById(1);
            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
        }
        [Fact]
        public async Task GetUsers_ShouldReturnEmptyList_WhenNoUsersExist()
        {
            var options = new DbContextOptions<ApiShopContext>();

            var mockContext = new Mock<ApiShopContext>(options);

            mockContext.Setup(x => x.Users).ReturnsDbSet(new List<User>());

            var repository = new UserRepository(mockContext.Object);

            var result = await repository.GetUsers();
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public async Task Login_ShouldReturnNull_WhenPasswordIsIncorrect()
        {
            var options = new DbContextOptions<ApiShopContext>();
            var mockContext = new Mock<ApiShopContext>(options);
            var usersList = new List<User>
    {
        new User
        {
            Email = "exist@gmail.com",
            Password = "CorrectPassword123"
        }
    };

            mockContext.Setup(x => x.Users).ReturnsDbSet(usersList);

            var repository = new UserRepository(mockContext.Object);
           
            var result = await repository.Login("exist@gmail.com", "WrongPassword!!!");
            Assert.Null(result);
        }
    }
}
