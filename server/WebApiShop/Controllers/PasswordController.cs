using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class PasswordController : ControllerBase
    {
        private readonly IPasswordServices _passwordServices;
        public PasswordController(IPasswordServices passwordServices)
        {
            _passwordServices = passwordServices;
        }
       
        [HttpPost("PasswordScore")]
        public ActionResult<int> PasswordScore([FromBody] string password)
        {
            int strength = _passwordServices.PasswordScore(password);
            return Ok(strength);
        }

    }
}
