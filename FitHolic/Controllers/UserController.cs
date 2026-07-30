using FitHolic.DTO;
using FitHolic.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHolic.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UserController : Controller
    {
        private readonly FitHolicDbContext _context;
        private readonly IConfiguration _configuration;

        public UserController(FitHolicDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }
        [HttpPost("create-user")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
        {
            try
            {
                bool userExists = await _context.Users
                    .AnyAsync(u => u.Username.ToLower() == dto.Username.ToLower() || u.Email.ToLower() == dto.Email.ToLower());

                var validator = new CreateUserValidator(_context);
                var validationResult = await validator.ValidateAsync(dto);

                if (!validationResult.IsValid)
                {
                    var firstError = validationResult.Errors.First();

                    return BadRequest(new ErrorResponse(
                        statusCode: 400,
                        errorType: firstError.ErrorCode,
                        message: firstError.ErrorMessage
                    ));
                }

                string secureHash = BCrypt.Net.BCrypt.HashPassword(dto.Password);

                var newUser = new User
                {
                    Username = dto.Username,
                    Email = dto.Email,
                    PasswordHash = secureHash,
                    FirstName = dto.FirstName,
                    LastName = dto.LastName,
                    RoleId = 1,
                    IsActive = true,
                    CreatedAt = DateTime.Parse(DateTime.Now.ToString("yyyy-MM-dd HH:mm")),

                    PhoneNumber = dto.PhoneNumber,
                    IsTwoFactorEnabled = true
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                return Ok(new { message = "Admin user successfully created via secure API!" });
            }
            catch (Exception)
            {
                return StatusCode(500, new ErrorResponse(500, "DATABASE_ERROR", "An internal error occurred while saving."));
            }
        }
    }
}
