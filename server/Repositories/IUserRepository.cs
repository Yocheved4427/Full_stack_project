using Entities;

namespace Repositories
{
    public interface IUserRepository
    {
        
        Task<User?> GetUserById(int id);
        Task<User?> Login(string email,string password);
        Task<User?> Register(User user);
        Task Update(int id, User updateUser);
        Task<IEnumerable<User>> GetUsers();
    }
}