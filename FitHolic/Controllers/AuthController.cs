using FitHolic.Class;
using FitHolic.DTO;
using FitHolic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHolic.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly FitHolicDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly GenerateTokenJwt _generateToken;
        private readonly SendEmailOtp _sendOTP;

        public AuthController(FitHolicDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _sendOTP = new SendEmailOtp(configuration);
            _generateToken = new GenerateTokenJwt(context, configuration);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var validator = new LoginValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage
                ));
            }

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == request.Username);

            if (user.IsTwoFactorEnabled)
            {
                var random = new Random();
                string generatedOtp = random.Next(100000, 999999).ToString();

                var newOtp = new UserOTP
                {
                    UserId = user.Id,
                    OtpCode = generatedOtp,
                    ExpiryTime = DateTime.UtcNow.AddMinutes(5),
                    IsUsed = false
                };
                _context.UserOtps.Add(newOtp);
                await _context.SaveChangesAsync();

                //string maskedPhone = !string.IsNullOrEmpty(user.PhoneNumber)
                //? StringExtensions.MaskContactNo(user.PhoneNumber)
                //: "No Phone Registered";

                _sendOTP.SendOTP(user.Email, generatedOtp);

                int atIndex = user.Email.IndexOf("@");
                string maskedEmail = user.Email.Substring(0, Math.Min(2, atIndex)) + "******" + user.Email.Substring(atIndex);

                return Ok(new LoginSuccessResponse
                {
                    Message = "OTP sent to your registered email.",
                    EmailHint = maskedEmail,
                    UserId = user.Id
                });
            }

            if (_generateToken == null)
            {
                return StatusCode(500, "Token service is not initialized.");
            }

            var token = _generateToken.GenerateJwtToken(user);
            return Ok(new { Requires2FA = false, Token = token });
        }

        [HttpPost("verify-otp")]
        public async Task<IActionResult> VerifyOtp([FromBody] VerifyOtpRequest request)
        {
            var currentUtcTime = DateTime.UtcNow;
            var latestOtp = await _context.UserOtps
                .Where(o => o.UserId == request.UserId && o.IsUsed == false && o.ExpiryTime >= currentUtcTime)
                .OrderByDescending(o => o.ExpiryTime)
                .FirstOrDefaultAsync();

            var validator = new VerifyOtpValidator(_context);
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();

                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage
                ));
            }

            if (latestOtp != null)
                latestOtp.IsUsed = true;

            await _context.SaveChangesAsync();

            var user = await _context.Users.FindAsync(request.UserId);

            var accessToken = _generateToken.GenerateJwtToken(user);
            var refreshToken = GenerateToken.GenerateRefreshToken();

            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Login Successful!",
                AccessToken = accessToken,
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        public async Task<IActionResult> Refresh([FromBody] TokenRequestDto tokenDto)
        {
            if (tokenDto == null) return BadRequest("Invalid client request");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.RefreshToken == tokenDto.RefreshToken);

            if (user == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
            {
                return Unauthorized("Session expired. Please log in again.");
            }

            var newAccessToken = _generateToken.GenerateJwtToken(user); var newRefreshToken = GenerateToken.GenerateRefreshToken();

            user.RefreshToken = newRefreshToken;
            user.RefreshTokenExpiryTime = DateTime.UtcNow.AddDays(7); await _context.SaveChangesAsync();

            return Ok(new
            {
                AccessToken = newAccessToken,
                RefreshToken = newRefreshToken
            });
        }
    }
}