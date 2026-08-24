using ClosedXML.Excel;
using FitHolic.DTO;
using FitHolic.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OutputCaching;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;

namespace FitHolic.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/expenses")]
    public class ExpensesController : ControllerBase
    {
        private readonly FitHolicDbContext _context;
        private readonly IOutputCacheStore _cacheStore;

        public ExpensesController(FitHolicDbContext context, IOutputCacheStore cacheStore)
        {
            _context = context;
            _cacheStore = cacheStore;
        }

        // ==========================================
        // 1. READ ENDPOINTS (WITH CACHING + RATE LIMITING)
        // ==========================================

        [HttpGet("list")]
        [EnableRateLimiting("ConcurrencyPolicy")]
        [OutputCache(PolicyName = "ExpensesListCache")] // Caches paged & filtered responses
        public async Task<IActionResult> GetPagedExpenses(
            [FromQuery] string? searchTerm = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "date_desc")
        {
            if (pageNumber < 1) pageNumber = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.GymExpenses.AsNoTracking().AsQueryable();

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
                TotalPages: totalPages == 0 ? 1 : totalPages,
                CurrentPage: pageNumber,
                PageSize: pageSize,
                Data: pagedData
            ));
        }

        [HttpGet("{id:int}")]
        [EnableRateLimiting("ConcurrencyPolicy")]
        [OutputCache(Duration = 300)] // Cache individual report details for 5 minutes
        public async Task<IActionResult> GetExpenseReportById(int id)
        {
            var report = await _context.GymExpenses
                .AsNoTracking()
                .FirstOrDefaultAsync(e => e.Id == id);

            if (report == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"Ang Expenses Report na may ID {id} ay hindi mahanap."));
            }

            return Ok(report);
        }

        // ==========================================
        // 2. WRITE ENDPOINTS (WITH RATE LIMITING + CACHE EVICTION)
        // ==========================================

        [HttpPost("save-expenses")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(UpdateSavingSuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateExpenses([FromBody] CreateExpensesRequest request)
        {
            var validator = new CreateExpensesRequestValidator();
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

            var expenseReport = new GymExpensesReport
            {
                ReportName = request.ReportName,
                CashPayment = request.CashPayment,
                OnlinePayment = request.OnlinePayment ?? 0,
                Expenses = request.Expenses,
                Payroll = request.Payroll,
                Electricity = request.Electricity,
                TotalExpenses = request.TotalExpenses
            };

            _context.GymExpenses.Add(expenseReport);
            await _context.SaveChangesAsync();

            // Evict/clear cached lists so fresh data appears immediately
            await _cacheStore.EvictByTagAsync("expenses-data", default);

            return Ok(new
            {
                message = "Successfully saved the expenses report.",
                savedIds = expenseReport.Id
            });
        }

        [HttpPost("update/{id:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateExpenseReport(int id, [FromBody] UpdateExpensesRequest request)
        {
            var validator = new UpdateExpensesRequestValidator();
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

            var existingReport = await _context.GymExpenses.FindAsync(id);
            if (existingReport == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"Expenses report with ID {id} not found."));
            }

            existingReport.ReportName = request.ReportName;
            existingReport.CashPayment = request.CashPayment;
            existingReport.OnlinePayment = request.OnlinePayment ?? 0;
            existingReport.Expenses = request.Expenses;
            existingReport.Payroll = request.Payroll;
            existingReport.Electricity = request.Electricity;
            existingReport.TotalExpenses = request.TotalExpenses;
            existingReport.LastDateUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            // Evict/clear cached lists so updated data reflects across lists/queries
            await _cacheStore.EvictByTagAsync("expenses-data", default);

            return Ok(new SuccessfulResponse { message = "Report successfully updated!" });
        }

        [HttpDelete("delete/{id:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        public async Task<IActionResult> DeleteExpense(int id)
        {
            var expense = await _context.GymExpenses.FindAsync(id);

            if (expense == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"Expense with ID {id} was not found."));
            }

            _context.GymExpenses.Remove(expense);
            await _context.SaveChangesAsync();

            // Invalidate expenses cache list after deletion
            await _cacheStore.EvictByTagAsync("expenses-list-cache", HttpContext.RequestAborted);

            return Ok(new { Message = $"Expense with ID {id} was successfully deleted." });
        }

		[HttpPost("download")]
		[EnableRateLimiting("DownloadPolicy")]
		[ProducesResponseType(StatusCodes.Status200OK)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
		[ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
		public async Task<IActionResult> DownloadExpenseReports([FromBody] DownloadExpensesRequest request)
		{
			if (request?.ExpenseIds == null || !request.ExpenseIds.Any())
			{
				return BadRequest(new ErrorResponse(400, "INVALID_REQUEST", "Please select at least one expense report to download."));
			}

			var expenses = await _context.GymExpenses
				.Where(e => request.ExpenseIds.Contains(e.Id))
				.ToListAsync();

			if (!expenses.Any())
			{
				return NotFound(new ErrorResponse(404, "NOT_FOUND", "No matching expense records found for the selected IDs."));
			}

			// KAPAG ISA LANG ANG SINELECT: Download bilang single Excel File (.xlsx)
			if (expenses.Count == 1)
			{
				var expense = expenses.First();
				var fileBytes = GenerateExpenseExcelBytes(expense);
				string fileName = $"{SanitizeFileName(expense.ReportName)}.xlsx";

				return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
			}

			// KAPAG MULTIPLE ANG SINELECT: Download bilang ZIP File na may hiwa-hiwalay na Excel files
			using (var zipStream = new MemoryStream())
			{
				using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
				{
					foreach (var expense in expenses)
					{
						var fileBytes = GenerateExpenseExcelBytes(expense);
						var entryName = $"{SanitizeFileName(expense.ReportName)}_{expense.Id}.xlsx";

						var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
						using (var entryStream = entry.Open())
						{
							await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
						}
					}
				}

				zipStream.Position = 0;
				string zipFileName = $"FitHolic_Expense_Reports_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

				return File(zipStream.ToArray(), "application/zip", zipFileName);
			}
		}

		private byte[] GenerateExpenseExcelBytes(GymExpensesReport expense)
		{
			using (var workbook = new XLWorkbook())
			{
				var worksheet = workbook.Worksheets.Add("Expense Details");

				// 1. Header Details (Base sa UI Screenshot)
				worksheet.Cell(1, 1).Value = expense.ReportName; // Halimbawa: "Expenses for May"
				worksheet.Cell(1, 1).Style.Font.Bold = true;
				worksheet.Cell(1, 1).Style.Font.FontSize = 16;

				worksheet.Cell(2, 1).Value = expense.CreatedAt.ToString("MMM d, yyyy - HH:mm:ss");
				worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

				// 2. Table Headers (Base sa Columns sa Screenshot)
				string[] headers = new string[]
				{
			"Cash Payment",
			"Online Payment",
			"Expenses",
			"Payroll",
			"Electricity",
			"Grand Total"
				};

				int headerRowIndex = 4;
				for (int col = 0; col < headers.Length; col++)
				{
					var cell = worksheet.Cell(headerRowIndex, col + 1);
					cell.Value = headers[col];
					cell.Style.Font.Bold = true;
					cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F9F9F9");
					cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
					cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
					cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
				}

				// 3. Values Row
				int valueRowIndex = 5;

				worksheet.Cell(valueRowIndex, 1).Value = expense.CashPayment;
				worksheet.Cell(valueRowIndex, 2).Value = expense.OnlinePayment;
				worksheet.Cell(valueRowIndex, 3).Value = expense.Expenses; // o ikaw na pumalit sa tamang property
				worksheet.Cell(valueRowIndex, 4).Value = expense.Payroll;
				worksheet.Cell(valueRowIndex, 5).Value = expense.Electricity;
				worksheet.Cell(valueRowIndex, 6).Value = expense.TotalExpenses;

				// Apply Formatting at Border sa bawat Cell
				for (int col = 1; col <= headers.Length; col++)
				{
					var cell = worksheet.Cell(valueRowIndex, col);
					cell.Style.NumberFormat.Format = "₱#,##0.00";
					cell.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
					cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
					cell.Style.Border.OutsideBorderColor = XLColor.FromHtml("#E0E0E0");
				}

				worksheet.Columns().AdjustToContents();

				using (var stream = new MemoryStream())
				{
					workbook.SaveAs(stream);
					return stream.ToArray();
				}
			}
		}

		private string SanitizeFileName(string fileName)
		{
			foreach (char c in Path.GetInvalidFileNameChars())
			{
				fileName = fileName.Replace(c, '_');
			}
			return fileName;
		}
	}
}