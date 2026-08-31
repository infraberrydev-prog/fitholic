using Microsoft.AspNetCore.Mvc;
using System.Reflection;

namespace FitHolic.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly FitHolicDbContext _context; // Palitan ng totoong DbContext class name mo

        public HealthController(FitHolicDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> GetStatus()
        {
            bool isDbConnected = false;

            try
            {
                // Mabilis na database check (executes SELECT 1)
                isDbConnected = await _context.Database.CanConnectAsync();
            }
            catch
            {
                isDbConnected = false;
            }

            // Kunin ang API Version mula sa Assembly
            var apiVersion = Assembly.GetExecutingAssembly().GetName().Version?.ToString() ?? "1.0.0";

            var statusResult = new
            {
                status = isDbConnected ? "Healthy" : "Degraded",
                version = apiVersion,
                database = isDbConnected ? "Connected" : "Disconnected",
                timestamp = DateTime.Now.ToString("MM-dd-yyyy HH:mm tt")
            };

            if (!isDbConnected)
            {
                return StatusCode(StatusCodes.Status503ServiceUnavailable, statusResult);
            }

            return Ok(statusResult);
        }
    }
}
