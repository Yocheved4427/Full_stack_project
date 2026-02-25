using Entities;
using DTOs;
namespace Services
{
    public interface IUserServices
    {
        Task<UserDTO?> GetUserById(int id);
        Task<UserDTO?> Login(ExistingUserDTO existingUser);
        Task<UserDTO?> Register(UserDTO user);
        Task<bool> Update(int id, UserDTO updateUser);
        Task<string?> ChangePassword(int userId, string currentPassword, string newPassword);
        Task<IEnumerable<UserDTO>> GetUsers();

    }
}