using Entities;
using Microsoft.EntityFrameworkCore;
namespace Repositories


{

    public class UserRepository : IUserRepository
    {
        private readonly ApiShopContext _context;
        public UserRepository(ApiShopContext context)
        {
            _context = context;
        }
        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }
        public async Task<User?> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email))
                return null;

            var normalizedEmail = email.Trim().ToLower();
            var incomingPassword = password?.Trim() ?? string.Empty;

            var user = await _context.Users
                .Include(user => user.Orders)
                .FirstOrDefaultAsync(user => user.Email.ToLower() == normalizedEmail);

            if (user == null)
                return null;

            var storedPassword = user.Password?.Trim() ?? string.Empty;
            return storedPassword == incomingPassword ? user : null;

        }
        public async Task<User?> Register(User user)
        {
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;

        }
        public async Task Update(int id, User updateUser)
        {
            _context.Users.Update(updateUser);
            await _context.SaveChangesAsync();
        }
       
    }
}
