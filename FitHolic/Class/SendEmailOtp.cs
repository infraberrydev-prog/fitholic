using MailKit.Security;
using MimeKit;
using System.Text;
using System.Text.Json;

namespace FitHolic.Class
{
    public class SendEmailOtp
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public SendEmailOtp(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = new HttpClient();
        }

        public async Task SendOTPAsync(string targetEmail, string otpCode)
        {
            try
            {
                string apiKey = _configuration["Resend:ApiKey"] ?? "";

                if (string.IsNullOrEmpty(apiKey))
                {
                    Console.WriteLine("[EMAIL ERROR] Resend API Key is missing!");
                    return;
                }

                _httpClient.DefaultRequestHeaders.Clear();
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                var payload = new
                {
                    from = "Support Team <onboarding@resend.dev>",
                    to = new[] { targetEmail },
                    subject = "Your one-time password",
                    html = $@"
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
                    </div>"
                };

                var jsonContent = new StringContent(
                    JsonSerializer.Serialize(payload),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("https://api.resend.com/emails", jsonContent);

                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine("[EMAIL SUCCESS] OTP email sent successfully via Resend!");
                }
                else
                {
                    string errorResponse = await response.Content.ReadAsStringAsync();
                    Console.WriteLine($"[EMAIL ERROR] Resend API failed: {response.StatusCode} - {errorResponse}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[EMAIL ERROR] Failed to send email: {ex.Message}");
            }
        }
    }
}


#region Backup for SMTP
//using MailKit.Net.Smtp;
//using MailKit.Security;
//using MimeKit;

//namespace FitHolic.Class
//{
//    public class SendEmailOtp
//    {
//        private readonly IConfiguration _configuration;

//        public SendEmailOtp(IConfiguration configuration)
//        {
//            _configuration = configuration;
//        }

//        public async Task SendOTPAsync(string targetEmail, string otpCode)
//        {
//            try
//            {
//                string smtpServer = _configuration["SmtpSettings:Server"] ?? "smtp.gmail.com";
//                int smtpPort = int.Parse(_configuration["SmtpSettings:Port"] ?? "465");
//                string senderEmail = _configuration["SmtpSettings:SenderEmail"] ?? "";
//                string senderName = _configuration["SmtpSettings:SenderName"] ?? "Support Team";
//                string password = _configuration["SmtpSettings:Password"] ?? "";

//                var message = new MimeMessage();
//                message.From.Add(new MailboxAddress(senderName, senderEmail));
//                message.To.Add(new MailboxAddress("", targetEmail));
//                message.Subject = "Your one-time password";

//                var bodyBuilder = new BodyBuilder
//                {
//                    HtmlBody = $@"
//                    <div style=""font-family: 'Segoe UI', Tahoma, Geneva, Verdana, sans-serif; color: #333333; max-width: 600px; margin: 0 auto; padding: 20px; line-height: 1.6;"">
//                        <p style=""font-size: 16px;"">Hello,</p>
//                        <p style=""font-size: 15px; margin-bottom: 25px;"">
//                            To verify your identity and continue signing in to the FitHolic Fitness Club, 
//                            please enter the following one-time password (OTP):
//                        </p>
//                        <p style=""font-size: 16px; margin-bottom: 10px;"">Your one-time password (OTP) is:</p>
//                        <div style=""font-size: 36px; font-weight: bold; letter-spacing: 4px; color: #1a1a1a; margin-bottom: 30px; margin-top: 5px;"">
//                            {otpCode}
//                        </div>
//                        <p style=""font-size: 14px; color: #555555; margin-bottom: 15px;"">
//                            The code is valid for 5 minutes and may only be used once.
//                        </p>
//                        <p style=""font-size: 14px; color: #555555; margin-bottom: 15px;"">
//                            For security purposes, do not share this code with anyone.
//                        </p>
//                        <p style=""font-size: 15px; margin-bottom: 5px;"">Thank you,</p>
//                        <p style=""font-size: 15px; font-weight: 500; margin-top: 0;"">{senderName}</p>
//                    </div>"
//                };

//                message.Body = bodyBuilder.ToMessageBody();

//                using (var client = new SmtpClient())
//                {
//                    await client.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
//                    await client.AuthenticateAsync(senderEmail, password);
//                    await client.SendAsync(message);
//                    await client.DisconnectAsync(true);
//                }
//            }
//            catch (Exception ex)
//            {
//                Console.WriteLine($"[EMAIL ERROR] Failed to send email: {ex.Message}");
//            }
//        }
//    }
//}
#endregion