using FitHolic.DTO;
using FitHolic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FitHolic.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ExpensesController : ControllerBase
    {
        private readonly FitHolicDbContext _context;

        public ExpensesController(FitHolicDbContext context)
        {
            _context = context;
        }

        [HttpPost("save-expenses")]
        public async Task<IActionResult> CreateExpenses([FromBody] CreateExpensesRequest request)
        {
            var validator = new CreateExpensesRequestValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();
                return BadRequest(new
                {
                    statusCode = 400,
                    errorType = firstError.ErrorCode,
                    message = firstError.ErrorMessage
                });
            }

            var expenseReport = new GymExpensesReport
            {
                ReportName = request.ReportName,
                Payroll = request.Payroll,
                OnlinePayment = request.OnlinePayment ?? 0,
                Electricity = request.Electricity,
                Water = request.Water,
                OtherUtilities = request.OtherUtilities,
                TotalExpenses = request.TotalExpenses
            };

            _context.GymExpenses.Add(expenseReport);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = "Successfully saved the expenses report.",
                SavedId = expenseReport.Id
            });
        }

        [HttpGet("list")]
        public async Task<IActionResult> GetPagedExpenses(
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "date_desc")
        {
            var query = _context.GymExpenses.AsQueryable();

            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                string lowerSearch = searchTerm.ToLower();
                query = query.Where(e => e.ReportName.ToLower().Contains(lowerSearch));
            }

            query = sortBy switch
            {
                "date_asc" => query.OrderBy(e => e.CreatedAt),
                "amount_desc" => query.OrderByDescending(e => e.TotalExpenses),
                "amount_asc" => query.OrderBy(e => e.TotalExpenses),
                _ => query.OrderByDescending(e => e.CreatedAt)
            };

            int totalCount = await query.CountAsync();
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            var pagedData = await query
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .Select(e => new ExpensesListDto(
                    e.Id,
                    $"EP{e.Id:D5}",
                    e.ReportName,
                    e.TotalExpenses,
                    e.CreatedAt,
                    null
                ))
                .ToListAsync();

            return Ok(new ExpensesPagedResponse(
                TotalCount: totalCount,
                TotalPages: totalPages,
                CurrentPage: pageNumber,
                PageSize: pageSize,
                Data: pagedData
            ));
        }

        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetExpenseReportById(int id)
        {
            var report = await _context.GymExpenses.FindAsync(id);

            if (report == null)
            {
                return NotFound(new { Message = $"Ang Expenses Report na may ID {id} ay hindi mahanap." });
            }

            return Ok(report);
        }

        [HttpPost("update/{id:int}")]
        public async Task<IActionResult> UpdateExpenseReport(int id, [FromBody] UpdateExpensesRequest request)
        {
            var validator = new UpdateExpensesRequestValidator();
            var validationResult = await validator.ValidateAsync(request);

            if (!validationResult.IsValid)
            {
                var firstError = validationResult.Errors.First();
                return BadRequest(new
                {
                    statusCode = 400,
                    errorType = firstError.ErrorCode,
                    message = firstError.ErrorMessage
                });
            }

            var existingReport = await _context.GymExpenses.FindAsync(id);
            if (existingReport == null)
            {
                return NotFound(new { Message = $"Expenses report with ID {id} not found." });
            }

            existingReport.ReportName = request.ReportName;
            existingReport.Payroll = request.Payroll;

            existingReport.OnlinePayment = request.OnlinePayment ?? 0;

            existingReport.Electricity = request.Electricity;
            existingReport.Water = request.Water;
            existingReport.OtherUtilities = request.OtherUtilities;
            existingReport.TotalExpenses = request.TotalExpenses;

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Report successfully updated!" });
        }
    }
}
