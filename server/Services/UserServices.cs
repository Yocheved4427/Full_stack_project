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
            var userEntity = await _repository.Login(existingUser.Email, existingUser.Password);

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
            int passScore = _passwordServices.PasswordScore(updateUser.Password);
            if (passScore < 2)
                return false;
            User user = _mapper.Map<User>(updateUser);
            await _repository.Update(id, user);
            return true;
        }
        

        public async Task<IEnumerable<UserDTO>> GetUsers()
        {
            return _mapper.Map<IEnumerable<User>, IEnumerable<UserDTO>>(await _repository.GetUsers());
            
        }
    }   
}
