using Entities;
using Microsoft.EntityFrameworkCore;
using Repositories;


namespace TestProject
{
    public class UserRepositoyIntegrationTests : IDisposable
    {
        private readonly DatabaseFixture _fixture;
        private readonly ApiShopContext _dbContext;
        private readonly UserRepository _userRepository;
        public UserRepositoyIntegrationTests()
        {
            _fixture = new DatabaseFixture();
            _dbContext = _fixture.Context;
            _userRepository = new UserRepository(_dbContext);
        }
        public void Dispose()
        {
            _fixture.Dispose();
        }

        [Fact]
        public async Task Register_ShouldAddUserToDatabase_WhenUserIsValid()
        {

            _dbContext.Users.RemoveRange(_dbContext.Users);
            await _dbContext.SaveChangesAsync();
            var userToAdd = new User
            {
                Email = "newUser@gmail.com",
                FirstName = "Test",
                LastName = "User",
                Password = "SecurePassword123"
            };

            var result = await _userRepository.Register(userToAdd);
            Assert.NotNull(result);
            Assert.NotEqual(0, result.UserId);
            Assert.Equal("Test", result.FirstName);

            var userInDb = await _dbContext.Users.FindAsync(result.UserId);
            Assert.NotNull(userInDb);
            Assert.Equal("newUser@gmail.com", userInDb.Email);
        }
        [Fact]
        public async Task GetUsers_ShouldReturnAddedUsers()
        {
            var user1 = new User { Email = "user1@test.com", FirstName = "Get1", LastName = "User", Password = "123" };
            var user2 = new User { Email = "user2@test.com", FirstName = "Get2", LastName = "User", Password = "123" };

            await _dbContext.Users.AddRangeAsync(user1, user2);
            await _dbContext.SaveChangesAsync();

            var result = await _userRepository.GetUsers();

            Assert.NotNull(result);
            Assert.NotEmpty(result); 

            Assert.Contains(result, u => u.Email == user1.Email);
            Assert.Contains(result, u => u.Email == user2.Email);
        }
        [Fact]
        public async Task Update_ShouldModifyUserInDatabase()
        {
   
            var originalUser = new User
            {
                Email = "original@test.com",
                FirstName = "OriginalName",
                LastName = "User",
                Password = "123"
            };

            await _dbContext.Users.AddAsync(originalUser);
            await _dbContext.SaveChangesAsync();

            
            _dbContext.ChangeTracker.Clear();

            
            var userWithUpdates = new User
            {
                UserId = originalUser.UserId, 
                Email = originalUser.Email,
                FirstName = "UpdatedName",
                LastName = "User",
                Password = "123"
            };

            await _userRepository.Update(originalUser.UserId, userWithUpdates);
            var userInDb = await _dbContext.Users.AsNoTracking()
                                           .FirstOrDefaultAsync(u => u.UserId == originalUser.UserId);

            Assert.NotNull(userInDb);
            Assert.Equal("UpdatedName", userInDb.FirstName); 
        }
        [Fact]
        public async Task Login_ShouldReturnUser_WhenCredentialsAreCorrect()
        {
            var userToAdd = new User
            {
                Email = "user@login.com",
                Password = "MySecretPassword",
                FirstName = "Integration",
                LastName = "User",
                Orders = new List<Order>
        {
            new Order
            {
                OrderDate = DateOnly.FromDateTime(DateTime.Now),
                OrderSum = 99 
            }
        }
            };

            await _dbContext.Users.AddAsync(userToAdd);
            await _dbContext.SaveChangesAsync();

           
            _dbContext.ChangeTracker.Clear();

            var result = await _userRepository.Login("user@login.com", "MySecretPassword");

            Assert.NotNull(result);
            Assert.Equal("user@login.com", result.Email);

            Assert.NotNull(result.Orders);
            Assert.NotEmpty(result.Orders);

            
            Assert.Equal(99, result.Orders.First().OrderSum);
        }
        [Fact]
        public async Task GetUserById_ShouldReturnUser_WhenIdExists()
        {
 
            var userToAdd = new User
            {
                Email = $"find_{Guid.NewGuid()}@test.com", // אימייל ייחודי למניעת התנגשויות
                FirstName = "Integration",
                LastName = "Find",
                Password = "123"
            };
           
            _dbContext.Users.Add(userToAdd);
            await _dbContext.SaveChangesAsync();

            _dbContext.ChangeTracker.Clear();

            var result = await _userRepository.GetUserById(userToAdd.UserId);

            Assert.NotNull(result); 
            Assert.Equal(userToAdd.UserId, result.UserId);
            Assert.Equal(userToAdd.Email, result.Email);
        }
        [Fact]
        public async Task GetUsers_ShouldReturnEmptyList_WhenNoUsersExist()
        {
            _dbContext.Users.RemoveRange(_dbContext.Users);
            await _dbContext.SaveChangesAsync();
            var result = await _userRepository.GetUsers();
            Assert.NotNull(result);
            Assert.Empty(result);
        }
        [Fact]
        public async Task Login_ShouldReturnNull_WhenPasswordIsIncorrect()
        {

            _dbContext.Orders.RemoveRange(_dbContext.Orders);
            _dbContext.Users.RemoveRange(_dbContext.Users);
            await _dbContext.SaveChangesAsync();


            var validUser = new User
            {
                Email = "exist@gmail.com",
                Password = "CorrectPassword123", 
                FirstName = "Test",
                LastName = "User"
            };

            await _dbContext.Users.AddAsync(validUser);
            await _dbContext.SaveChangesAsync();
            _dbContext.ChangeTracker.Clear();
            var result = await _userRepository.Login("exist@gmail.com", "WrongPassword!!!");
            Assert.Null(result);
        }
    }
}
