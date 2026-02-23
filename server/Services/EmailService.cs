using System.Net;
using System.Net.Mail;
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

                // For demo purposes, if no credentials are configured, we'll just log and return success
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    Console.WriteLine($"[EMAIL SIMULATION] Order Confirmation Email");
                    Console.WriteLine($"To: {toEmail}");
                    Console.WriteLine($"Customer: {customerName}");
                    Console.WriteLine($"Order #: {orderNumber}");
                    Console.WriteLine($"Total: ${orderTotal}");
                    Console.WriteLine($"Items: {orderItems}");
                    Console.WriteLine("Note: Configure SMTP credentials in appsettings.json to send real emails");
                    return;
                }

                // Create email message
                var mailMessage = new MailMessage
                {
                    From = new MailAddress(senderEmail, senderName),
                    Subject = $"Order Confirmation - {orderNumber}",
                    Body = GenerateEmailBody(customerName, orderNumber, orderTotal, orderItems),
                    IsBodyHtml = true
                };
                mailMessage.To.Add(toEmail);

                // Configure SMTP client
                using var smtpClient = new SmtpClient(smtpServer, smtpPort)
                {
                    Credentials = new NetworkCredential(username, password),
                    EnableSsl = true
                };

                // Send email
                await smtpClient.SendMailAsync(mailMessage);
                Console.WriteLine($"Order confirmation email sent successfully to {toEmail}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Failed to send email: {ex.Message}");
                // Log the error but don't throw - we don't want email failures to break the order process
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
                            <p>If you have any questions about your order, please contact our customer support.</p>
                            
                            <p>Best regards,<br/>The Vacation Shop Team</p>
                        </div>
                        <div class='footer'>
                            <p>© 2026 Vacation Shop. All rights reserved.</p>
                            <p>This is an automated message, please do not reply.</p>
                        </div>
                    </div>
                </body>
                </html>
            ";
        }
    }
}
