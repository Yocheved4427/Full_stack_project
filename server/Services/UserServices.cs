using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using AutoMapper;
using DTOs;
using Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Repositories;
namespace Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _repository;
        private readonly IPasswordServices _passwordServices;
        private readonly IMapper _mapper;
        private readonly IConfiguration _configuration;
        public UserServices(IUserRepository repository, IPasswordServices passwordServices, IMapper mapper, IConfiguration configuration)
        {
            _repository = repository;
            _passwordServices = passwordServices;
            _mapper = mapper;
            _configuration = configuration;
        }
      

       
        public async Task<UserDTO?> GetUserById(int id)
        {
            return _mapper.Map<User,UserDTO>(await _repository.GetUserById(id));
            
        }
        public async Task<UserDTO?> Login(ExistingUserDTO existingUser)
        {
            if (existingUser == null)
                return null;

            var normalizedEmail = existingUser.Email?.Trim() ?? string.Empty;
            var normalizedPassword = existingUser.Password?.Trim() ?? string.Empty;
            var userEntity = await _repository.Login(normalizedEmail, normalizedPassword);

            if (userEntity == null)
                return null;

            var userDTO = _mapper.Map<User, UserDTO>(userEntity);
            var tokenHandler = new JwtSecurityTokenHandler();
            var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Key"]);
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userDTO.Id.ToString()),
                new Claim(ClaimTypes.Email, userDTO.Email)
            };
            if (userDTO.IsAdmin)
            {
                claims.Add(new Claim(ClaimTypes.Role, "Admin"));
            }
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);

            userDTO.Token = tokenHandler.WriteToken(token);

            return userDTO;

        }
        public async Task<UserDTO?> Register(UserDTO user)
        {
            int passScore = _passwordServices.PasswordScore(user.Password);
            if (passScore < 2)
                throw new Exception("Password must be at least 8 characters and include a number and uppercase letter") ;
            
            User userEntity = _mapper.Map<User>(user);

            return _mapper.Map<UserDTO>(await _repository.Register(userEntity));
        }
        public async Task<bool> Update(int id, UserDTO updateUser)
        {
            var existingUser = await _repository.GetUserById(id);
            if (existingUser == null)
                return false;

            existingUser.FirstName = updateUser.FirstName;
            existingUser.LastName = updateUser.LastName;
            existingUser.Email = updateUser.Email;
            existingUser.IsAdmin = updateUser.IsAdmin;

            if (!string.IsNullOrWhiteSpace(updateUser.Password))
            {
                int passScore = _passwordServices.PasswordScore(updateUser.Password);
                if (passScore < 2)
                    return false;

                existingUser.Password = updateUser.Password;
            }

            await _repository.Update(id, existingUser);
            return true;
        }

        public async Task<string?> ChangePassword(int userId, string currentPassword, string newPassword)
        {
            var user = await _repository.GetUserById(userId);
            if (user == null)
                return "User not found";

            if (!string.Equals(user.Password?.Trim(), currentPassword?.Trim(), StringComparison.Ordinal))
                return "Current password is incorrect";

            int passScore = _passwordServices.PasswordScore(newPassword);
            if (passScore < 2)
                return "Password must be at least 8 characters and include a number and uppercase letter";

            user.Password = newPassword;
            await _repository.Update(userId, user);
            return null;
        }
        

        public async Task<IEnumerable<UserDTO>> GetUsers()
        {
            return _mapper.Map<IEnumerable<User>, IEnumerable<UserDTO>>(await _repository.GetUsers());
            
        }
    }   
}
