using Entities;
using Repositories;
using DTOs;
using AutoMapper;
namespace Services
{
    public class UserServices : IUserServices
    {
        private readonly IUserRepository _repository;
        private readonly IPasswordServices _passwordServices;
        private readonly IMapper _mapper;
        public UserServices(IUserRepository repository, IPasswordServices passwordServices, IMapper mapper)
        {
            _repository = repository;
            _passwordServices = passwordServices;
            _mapper = mapper;
        }
      

       
        public async Task<UserDTO?> GetUserById(int id)
        {
            return _mapper.Map<User,UserDTO>(await _repository.GetUserById(id));
            
        }
        public async Task<UserDTO?> Login(ExistingUserDTO existingUser)
        {
            return _mapper.Map<User,UserDTO>(await _repository.Login(existingUser.Email,existingUser.Password));
            
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
