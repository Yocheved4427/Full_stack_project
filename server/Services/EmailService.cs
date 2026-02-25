using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace Services
{
    public interface IEmailService
    {
        Task SendOrderConfirmationEmail(string toEmail, string customerName, string orderNumber, decimal orderTotal, string orderItems);
    }

    public class EmailService : IEmailService
    {
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOrderConfirmationEmail(string toEmail, string customerName, string orderNumber, decimal orderTotal, string orderItems)
        {
            try
            {
                var emailSettings = _configuration.GetSection("EmailSettings");
                var smtpServer = emailSettings["SmtpServer"];
                var smtpPort = int.Parse(emailSettings["SmtpPort"] ?? "587");
                var senderEmail = emailSettings["SenderEmail"];
                var senderName = emailSettings["SenderName"];
                var username = emailSettings["Username"];
                var password = emailSettings["Password"];

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress(customerName, toEmail));
                message.Subject = $"Order Confirmation - {orderNumber}";
                message.Body = new TextPart("html")
                {
                    Text = GenerateEmailBody(customerName, orderNumber, orderTotal, orderItems)
                };

                using var client = new SmtpClient();
                await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(username, password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                Console.WriteLine($"Email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                throw;
            }
        }

        private string GenerateEmailBody(string customerName, string orderNumber, decimal orderTotal, string orderItems)
        {
            return $@"
                <!DOCTYPE html>
                <html>
                <head>
                    <style>
                        body {{ font-family: Arial, sans-serif; line-height: 1.6; color: #333; }}
                        .container {{ max-width: 600px; margin: 0 auto; padding: 20px; }}
                        .header {{ background: linear-gradient(135deg, #667eea 0%, #764ba2 100%); color: white; padding: 30px; text-align: center; border-radius: 10px 10px 0 0; }}
                        .content {{ background: #f9f9f9; padding: 30px; border-radius: 0 0 10px 10px; }}
                        .order-details {{ background: white; padding: 20px; border-radius: 5px; margin: 20px 0; }}
                        .footer {{ text-align: center; margin-top: 30px; color: #666; font-size: 12px; }}
                        .total {{ font-size: 24px; color: #667eea; font-weight: bold; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <div class='header'>
                            <h1>Thank You for Your Purchase!</h1>
                        </div>
                        <div class='content'>
                            <p>Dear {customerName},</p>
                            <p>Thank you for choosing Vacation Shop! We're excited to confirm your order.</p>
                            <div class='order-details'>
                                <h2>Order Details</h2>
                                <p><strong>Order Number:</strong> {orderNumber}</p>
                                <p><strong>Order Date:</strong> {DateTime.Now:MMMM dd, yyyy}</p>
                                <p><strong>Total Amount:</strong> <span class='total'>${orderTotal:F2}</span></p>
                                <hr/>
                                <h3>Items:</h3>
                                <p>{orderItems}</p>
                            </div>
                            <p>We will send you another email when your booking is confirmed.</p>
                            <p>Best regards,<br/>The Vacation Shop Team</p>
                        </div>
                        <div class='footer'>
                            <p>© 2026 Vacation Shop. All rights reserved.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }
    }
}