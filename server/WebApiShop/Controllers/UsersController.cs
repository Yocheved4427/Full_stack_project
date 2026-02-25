using Microsoft.AspNetCore.Mvc;
using Services;
using Entities;
using DTOs;
// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly ILogger<UsersController> _logger;
        public UsersController(IUserServices userServices, ILogger<UsersController> logger)
        {
            _userServices = userServices;
            _logger = logger;
        }
        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDTO>>> Get()
        {
            IEnumerable<UserDTO> users = await _userServices.GetUsers();
            if (users != null && users.Any())
                return Ok(users);
            return NoContent();
        }


        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUserById(int id)
        {
            UserDTO user = await _userServices.GetUserById(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }
        [HttpPost("Login")]
        public async Task<ActionResult<UserDTO>> Login([FromBody] ExistingUserDTO existingUser)
        {

            UserDTO user = await _userServices.Login(existingUser);
            if (user== null)
                return Unauthorized("Invalid email or password");

            _logger.LogInformation($"login attempted id:{user.Id} email:{user.Email} first name:{user.FirstName} last name:{user.LastName}");
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDTO>> Register([FromBody] UserDTO user)
        {
            try
            {
                UserDTO user1 = await _userServices.Register(user);
            if (user1 == null)
                return BadRequest("Password");
            
                return CreatedAtAction(nameof(GetUserById), new { user.Id }, user);

            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PUT api/<Users>/5
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDTO updateUser)
        {

            bool success = await _userServices.Update(id, updateUser);
            if (!success)
                return BadRequest();
            return Ok(updateUser);
        }

        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDTO changePassword)
        {
            if (changePassword.UserId <= 0)
                return BadRequest(new { message = "Invalid user id" });

            if (string.IsNullOrWhiteSpace(changePassword.CurrentPassword) || string.IsNullOrWhiteSpace(changePassword.NewPassword))
                return BadRequest(new { message = "Current password and new password are required" });

            var errorMessage = await _userServices.ChangePassword(
                changePassword.UserId,
                changePassword.CurrentPassword,
                changePassword.NewPassword
            );

            if (errorMessage != null)
                return BadRequest(new { message = errorMessage });

            return Ok(new { message = "Password changed successfully" });
        }






    }
}


