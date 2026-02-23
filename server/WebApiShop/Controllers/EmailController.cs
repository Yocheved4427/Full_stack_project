using Microsoft.AspNetCore.Mvc;
using Services;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class EmailController : ControllerBase
    {
        private readonly IEmailService _emailService;

        public EmailController(IEmailService emailService)
        {
            _emailService = emailService;
        }

        [HttpPost("send-order-confirmation")]
        public async Task<IActionResult> SendOrderConfirmation([FromBody] EmailOrderRequest request)
        {
            Console.WriteLine($"Received email request for: {request.To}");
            Console.WriteLine($"Order number: {request.OrderNumber}");
            
            try
            {
                await _emailService.SendOrderConfirmationEmail(
                    request.To,
                    request.CustomerName,
                    request.OrderNumber,
                    request.OrderTotal,
                    request.OrderItems
                );

                Console.WriteLine("Email sent successfully");
                return Ok(new { success = true, message = "Email sent successfully" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Email error: {ex.Message}");
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }
    }

    public class EmailOrderRequest
    {
        public required string To { get; set; }
        public required string CustomerName { get; set; }
        public required string OrderNumber { get; set; }
        public decimal OrderTotal { get; set; }
        public required string OrderItems { get; set; }
    }
}
