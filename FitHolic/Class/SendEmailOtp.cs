using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Configuration;

namespace FitHolic.Class
{
    public class SendEmailOtp
    {
        private readonly IConfiguration _configuration;

        public SendEmailOtp(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public async Task SendOTP(string targetEmail, string otpCode)
        {
            try
            {
                string smtpServer = _configuration["SmtpSettings:Server"] ?? "smtp.gmail.com";
                int smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "587");
                string senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "";
                string senderName = _configuration["SmtpSettings:SenderName"] ?? "Support Team";
                string password = _configuration["SmtpSettings:Password"] ?? "";

                // 1. I-build ang MimeMessage (MailKit format)
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(senderName, senderEmail));
                message.To.Add(new MailboxAddress("", targetEmail));
                message.Subject = "Your one-time password";

                var bodyBuilder = new BodyBuilder
                {
                    HtmlBody = $@"
                    <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333333; max-width: 600px; margin: 0 auto; padding: 20px; line-height: 1.6;"">
                        <p style=""font-size: 16px;"">Hello,</p>
                        <p style=""font-size: 15px; margin-bottom: 25px;"">
                            To verify your identity and continue signing in to the FitHolic Fitness Club, 
                            please enter the following one-time password (OTP):
                        </p>
                        <p style=""font-size: 16px; margin-bottom: 10px;"">Your one-time password (OTP) is:</p>
                        <div style=""font-size: 36px; font-weight: bold; letter-spacing: 4px; color: #1a1a1a; margin-bottom: 30px; margin-top: 5px;"">
                            {otpCode}
                        </div>
                        <p style=""font-size: 14px; color: #555555; margin-bottom: 15px;"">
                            The code is valid for 5 minutes and may only be used once.
                        </p>
                        <p style=""font-size: 14px; color: #555555; margin-bottom: 15px;"">
                            For security purposes, do not share this code with anyone.
                        </p>
                        <p style=""font-size: 14px; color: #555555; margin-bottom: 35px;"">
                            If you did not request this verification, please contact your system administrator.
                        </p>
                        <p style=""font-size: 15px; margin-bottom: 5px;"">Thank you,</p>
                        <p style=""font-size: 15px; font-weight: 500; margin-top: 0;"">{senderName}</p>
                    </div>"
                };

                message.Body = bodyBuilder.ToMessageBody();

                // 2. Gamitin ang MailKit SmtpClient
                using (var client = new SmtpClient())
                {
                    // Kumonekta gamit ang Port 587 at STARTTLS
                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);

                    // I-authenticate ang credentials
                    await client.AuthenticateAsync(senderEmail, password);

                    // I-send ang email nang totoong Async
                    await client.SendAsync(message);
                    await client.DisconnectAsync(true);
                }
            }
            catch (Exception ex)
            {
                // ⚠️ HUWAG I-SWALLOW ANG ERROR! 
                // I-rethrow para malaman ng Controller kung may failure o gamitin ang Console/ILogger
                Console.WriteLine($"[SMTP ERROR]: {ex.Message}");
                throw new Exception($"Failed to send OTP Email: {ex.Message}", ex);
            }
        }
    }
}