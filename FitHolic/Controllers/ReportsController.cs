using ClosedXML.Excel;
using FitHolic.DTO;
using FitHolic.Models;
using FluentValidation;
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
    [Route("api/reports")]
    public class ReportsController : ControllerBase
    {
        private readonly FitHolicDbContext _context;
        private readonly IOutputCacheStore _cacheStore;

        // Visual tag identifier for clearing cache
        private const string ReportsCacheTag = "reports-cache-tag";

        public ReportsController(FitHolicDbContext context, IOutputCacheStore cacheStore)
        {
            _context = context;
            _cacheStore = cacheStore;
        }

        // Helper Method para hindi paulit-ulit ang pag-evict
        private async Task EvictReportsCacheAsync(CancellationToken cancellationToken = default)
        {
            await _cacheStore.EvictByTagAsync(ReportsCacheTag, cancellationToken);
        }

        // ==========================================
        // 1. READ ENDPOINTS (WITH CACHING)
        // ==========================================

        [HttpGet("get-all-reports")]
        // Pinagsama ang Policy (kung may custom setup sa Program.cs) at Tag para madaling i-evict
        [OutputCache(PolicyName = "ReportsListCache", Tags = [ReportsCacheTag])]
        [ProducesResponseType(typeof(PagedReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<PagedReportResponse>> GetReports(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.GymReports.AsNoTracking();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.ReportName.ToLower().Contains(search.ToLower()));
            }

            int totalRecords = await query.CountAsync();

            var reportList = await query
                .OrderBy(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(r => new ReportListDto(
                    r.Id,
                    $"MR{r.Id:D5}",
                    r.ReportName,
                    r.Rows.Where(row => string.IsNullOrWhiteSpace(row.OnlinePaymentMethod)).Sum(row => (decimal?)row.AmountToPay) ?? 0,
                    r.Rows.Where(row => !string.IsNullOrWhiteSpace(row.OnlinePaymentMethod)).Sum(row => (decimal?)row.AmountToPay) ?? 0,
                    r.CreatedAt,
                    r.LastDateUpdated
                ))
                .ToListAsync();

            int totalPages = (int)Math.Ceiling((double)totalRecords / pageSize);

            var response = new PagedReportResponse(
                Data: reportList,
                TotalRecords: totalRecords,
                CurrentPage: page,
                TotalPages: totalPages == 0 ? 1 : totalPages,
                PageSize: pageSize
            );

            return Ok(response);
        }

        [HttpGet("{id:int}/details")]
        // Nilagyan din ng Tag para ma-clear ito pag pinalitan ang specific report details
        [OutputCache(Duration = 300, Tags = [ReportsCacheTag])]
        [ProducesResponseType(typeof(ReportDetailsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult<ReportDetailsResponse>> GetReportById(int id)
        {
            var report = await _context.GymReports
                .Include(r => r.Rows)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (report == null)
            {
                return NotFound(new { Message = $"No Report with ID {id} found." });
            }

            var cashRows = report.Rows
                .Where(row => string.IsNullOrWhiteSpace(row.OnlinePaymentMethod))
                .Select(row => new ReportRowDetailsDto(
                    Id: row.Id,
                    CustomerName: row.CustomerName,
                    IsCustomerMember: row.IsCustomerMember,
                    DateOfEnrollment: row.DateOfEnrollment,
                    DailyPass: row.DailyPass,
                    MembershipFee: row.MembershipFee,
                    ContractDuration: row.ContractDuration,
                    MonthlyPayment: row.MonthlyPayment,
                    IncludePersonalTrainer: row.IncludePersonalTrainer,
                    PersonalTrainerPackage: row.PersonalTrainerPackage,
                    PaymentOption: row.PaymentOption,
                    AmountToPay: row.AmountToPay,
                    OnlinePaymentMethod: null,
                    ReferenceNumber: null
                )).ToList();

            var onlineRows = report.Rows
                .Where(row => !string.IsNullOrWhiteSpace(row.OnlinePaymentMethod))
                .Select(row => new ReportRowDetailsDto(
                    Id: row.Id,
                    CustomerName: row.CustomerName,
                    IsCustomerMember: row.IsCustomerMember,
                    DateOfEnrollment: row.DateOfEnrollment,
                    DailyPass: row.DailyPass,
                    MembershipFee: row.MembershipFee,
                    ContractDuration: row.ContractDuration,
                    MonthlyPayment: row.MonthlyPayment,
                    IncludePersonalTrainer: row.IncludePersonalTrainer,
                    PersonalTrainerPackage: row.PersonalTrainerPackage,
                    PaymentOption: row.PaymentOption,
                    AmountToPay: row.AmountToPay,
                    OnlinePaymentMethod: row.OnlinePaymentMethod,
                    ReferenceNumber: row.ReferenceNumber
                )).ToList();

            var response = new ReportDetailsResponse(
                ReportId: $"MR{report.Id:D5}",
                ReportName: report.ReportName,
                PaymentType: report.PaymentType,
                TotalAmount: report.TotalAmount,
                DateCreated: report.CreatedAt,
                CashRows: cashRows,
                OnlineRows: onlineRows
            );

            return Ok(response);
        }

        [HttpPost("download")]
        [EnableRateLimiting("DownloadPolicy")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DownloadMembershipReports([FromBody] DownloadReportsRequest request)
        {
            if (request?.ReportIds == null || !request.ReportIds.Any())
            {
                return BadRequest(new ErrorResponse(400, "INVALID_REQUEST", "Please select at least one report to download."));
            }

            var reports = await _context.GymReports
                .Include(r => r.Rows)
                .Where(r => request.ReportIds.Contains(r.Id))
                .ToListAsync();

            if (!reports.Any())
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", "No matching reports found for the selected IDs."));
            }

            // 1. Fetch Packages para sa Mapping (Package Code/Name -> Price Description)
            var packageLookup = await _context.Package
                .ToDictionaryAsync(p => p.Package, p => p.Price);

            // KAPAG ISA LANG ANG SINELECT
            if (reports.Count == 1)
            {
                var report = reports.First();
                var fileBytes = GenerateReportExcelBytes(report, packageLookup);
                string fileName = $"{SanitizeFileName(report.ReportName)}.xlsx";

                return File(fileBytes, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", fileName);
            }

            // KAPAG MULTIPLE ANG SINELECT (ZIP Download)
            using (var zipStream = new MemoryStream())
            {
                using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
                {
                    foreach (var report in reports)
                    {
                        var fileBytes = GenerateReportExcelBytes(report, packageLookup);
                        var entryName = $"{SanitizeFileName(report.ReportName)}_{report.Id}.xlsx";

                        var entry = archive.CreateEntry(entryName, CompressionLevel.Fastest);
                        using (var entryStream = entry.Open())
                        {
                            await entryStream.WriteAsync(fileBytes, 0, fileBytes.Length);
                        }
                    }
                }

                zipStream.Position = 0;
                string zipFileName = $"FitHolic_Selected_Reports_{DateTime.Now:yyyyMMdd_HHmmss}.zip";

                return File(zipStream.ToArray(), "application/zip", zipFileName);
            }
        }

        // ==========================================
        // HELPER METHODS FOR EXCEL GENERATION
        // ==========================================

        private byte[] GenerateReportExcelBytes(GymReport report, Dictionary<string, string> packageLookup)
        {
            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Report Details");

                // 1. Report Header
                worksheet.Cell(1, 1).Value = report.ReportName;
                worksheet.Cell(1, 1).Style.Font.Bold = true;
                worksheet.Cell(1, 1).Style.Font.FontSize = 16;

                worksheet.Cell(2, 1).Value = report.CreatedAt.ToString("MMM dd, yyyy - HH:mm:ss");
                worksheet.Cell(2, 1).Style.Font.FontColor = XLColor.Gray;

                // 2. Table Headers
                string[] headers = new string[]
                {
            "Name", "Date of Enrollment", "Is the customer member?", "Daily Pass",
            "Membership Fee", "Contract Duration", "Monthly Payment", "Personal Trainer",
            "Package", "Payment Option", "Amount to Pay", "Payment Method", "Reference No.", "Total Amount"
                };

                int headerRowIndex = 4;
                for (int col = 0; col < headers.Length; col++)
                {
                    var cell = worksheet.Cell(headerRowIndex, col + 1);
                    cell.Value = headers[col];
                    cell.Style.Font.Bold = true;
                    cell.Style.Fill.BackgroundColor = XLColor.FromHtml("#F5F5F5");
                    cell.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                    cell.Style.Border.OutsideBorderColor = XLColor.LightGray;
                }

                // 3. Child Rows Data
                int currentRow = 5;
                foreach (var row in report.Rows)
                {
                    worksheet.Cell(currentRow, 1).Value = row.CustomerName;
                    worksheet.Cell(currentRow, 2).Value = row.DateOfEnrollment?.ToString("MMM dd, yyyy") ?? "—";
                    worksheet.Cell(currentRow, 3).Value = row.IsCustomerMember ? "Yes" : "No";

                    worksheet.Cell(currentRow, 4).Value = row.DailyPass.HasValue ? row.DailyPass.Value : "—";
                    if (row.DailyPass.HasValue) worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 5).Value = row.MembershipFee > 0 ? row.MembershipFee : "—";
                    if (row.MembershipFee > 0) worksheet.Cell(currentRow, 5).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 6).Value = string.IsNullOrWhiteSpace(row.ContractDuration) ? "—" : row.ContractDuration;

                    worksheet.Cell(currentRow, 7).Value = row.MonthlyPayment > 0 ? row.MonthlyPayment : "—";
                    if (row.MonthlyPayment > 0) worksheet.Cell(currentRow, 7).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 8).Value = row.IncludePersonalTrainer ? "Yes" : "No";

                    // PACKAGE COLUMN MAPPING:
                    // Naghahanap sa Dictionary base sa `row.PersonalTrainerPackage` (hal. "30S-18K").
                    // Kapag nahanap, kukunin ang Price text ("30 Sessions - Php 18,000").
                    // Kapag wala o null, gagamitin ang raw string o "—".
                    string mappedPackageDisplay = "—";
                    if (!string.IsNullOrWhiteSpace(row.PersonalTrainerPackage))
                    {
                        if (packageLookup.TryGetValue(row.PersonalTrainerPackage, out var priceText))
                        {
                            mappedPackageDisplay = priceText;
                        }
                        else
                        {
                            mappedPackageDisplay = row.PersonalTrainerPackage;
                        }
                    }
                    worksheet.Cell(currentRow, 9).Value = mappedPackageDisplay;

                    worksheet.Cell(currentRow, 10).Value = string.IsNullOrWhiteSpace(row.PaymentOption) ? "—" : row.PaymentOption;

                    worksheet.Cell(currentRow, 11).Value = row.AmountToPay;
                    worksheet.Cell(currentRow, 11).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 12).Value = string.IsNullOrWhiteSpace(row.OnlinePaymentMethod) ? "Cash" : row.OnlinePaymentMethod;
                    worksheet.Cell(currentRow, 13).Value = string.IsNullOrWhiteSpace(row.ReferenceNumber) ? "—" : row.ReferenceNumber;

                    worksheet.Cell(currentRow, 14).Value = report.TotalAmount;
                    worksheet.Cell(currentRow, 14).Style.NumberFormat.Format = "₱#,##0.00";

                    // Row Borders
                    for (int col = 1; col <= headers.Length; col++)
                    {
                        worksheet.Cell(currentRow, col).Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                        worksheet.Cell(currentRow, col).Style.Border.OutsideBorderColor = XLColor.FromHtml("#E0E0E0");
                    }

                    currentRow++;
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

        // ==========================================
        // 2. MEMBER REPORTS WRITE (POST ENDPOINTS)
        // ==========================================

        [HttpPost("save-report")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(UpdateSavingSuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> CreateBulkReports([FromBody] GroupedBulkCreateReportRequest request)
        {
            // 1. Validation Logic
            if (request == null || request.Reports == null || !request.Reports.Any())
            {
                return BadRequest(new ErrorResponse(400, "INVALID_PAYLOAD", "Report payload cannot be empty."));
            }

            var gymReportsToSave = new List<GymReport>();

            foreach (var reportDto in request.Reports)
            {
                var allRowsForThisReport = new List<GymReportRow>();

                // Process Cash Rows (Tinitiyak na walang Online payment details)
                if (reportDto.CashRows != null && reportDto.CashRows.Any())
                {
                    var cashRows = reportDto.CashRows.Select(row => MapToGymReportRow(row, isOnline: false));
                    allRowsForThisReport.AddRange(cashRows);
                }

                // Process Online Rows
                if (reportDto.OnlineRows != null && reportDto.OnlineRows.Any())
                {
                    var onlineRows = reportDto.OnlineRows.Select(row => MapToGymReportRow(row, isOnline: true));
                    allRowsForThisReport.AddRange(onlineRows);
                }

                var newReport = new GymReport
                {
                    ReportName = reportDto.ReportName,
                    PaymentType = reportDto.PaymentType,
                    TotalAmount = reportDto.TotalAmount,
                    Rows = allRowsForThisReport
                };

                gymReportsToSave.Add(newReport);
            }

            // 2. Database Save
            _context.GymReports.AddRange(gymReportsToSave);
            await _context.SaveChangesAsync();

            // 3. Clear Output Cache para mag-reflect agad sa GET endpoints
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new UpdateSavingSuccessfulResponse
            {
                message = $"Successfully saved {gymReportsToSave.Count} reports.",
                savedIds = gymReportsToSave.Select(r => r.Id).ToList()
            });
        }

        // Private Helper Method para malinis at hindi paulit-ulit ang pag-map ng entity
        private static GymReportRow MapToGymReportRow(ReportRowDto row, bool isOnline)
        {
            DateOnly? finalEnrollmentDate = row.IsCustomerMember ? null : row.DateOfEnrollment;
            decimal finalMembershipFee = row.IsCustomerMember ? 0 : row.MembershipFee;

            return new GymReportRow
            {
                CustomerName = row.CustomerName,
                IsCustomerMember = row.IsCustomerMember,
                DateOfEnrollment = finalEnrollmentDate,
                MembershipFee = finalMembershipFee,
                DailyPass = null,
                ContractDuration = string.IsNullOrWhiteSpace(row.ContractDuration) ? "—" : row.ContractDuration,
                MonthlyPayment = row.MonthlyPayment,
                IncludePersonalTrainer = row.IncludePersonalTrainer,
                PersonalTrainerPackage = row.IncludePersonalTrainer ? row.PersonalTrainerPackage : "—",
                PaymentOption = row.PaymentOption,
                AmountToPay = row.AmountToPay,
                OnlinePaymentMethod = isOnline ? (string.IsNullOrWhiteSpace(row.OnlinePaymentMethod) ? null : row.OnlinePaymentMethod) : null,
                ReferenceNumber = isOnline ? (string.IsNullOrWhiteSpace(row.ReferenceNumber) ? null : row.ReferenceNumber) : null
            };
        }

        [HttpPost("update/{id:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(UpdateSingleReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateReport(int id, [FromBody] UpdateSingleReportRequest request)
        {
            var validator = new UpdateReportRequestValidator();
            var validatorResult = await validator.ValidateAsync(request);

            if (!validatorResult.IsValid)
            {
                var firstError = validatorResult.Errors.First();
                return BadRequest(new ErrorResponse(400, firstError.ErrorCode, firstError.ErrorMessage));
            }

            var existingReport = await _context.GymReports
                .Include(r => r.Rows)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReport == null)
            {
                return NotFound(new { Message = $"No Report with ID {id} found." });
            }

            existingReport.ReportName = request.ReportName;
            existingReport.LastDateUpdated = DateTime.Now;

            await _context.SaveChangesAsync();

            // Clear cache para mag-reflect agad ang bagong name
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new { Message = "Report successfully updated!" });
        }

        [HttpPost("save-non-member-report")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(UpdateSavingSuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> CreateBulkWalkInReports([FromBody] BulkCreateWalkInReportRequest request)
        {
            var validator = new BulkCreateWalkInReportRequestValidator();
            var validatorResult = await validator.ValidateAsync(request);

            if (!validatorResult.IsValid)
            {
                var firstError = validatorResult.Errors.First();
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage));
            }

            var walkInReportsToSave = new List<GymReport>();

            foreach (var reportDto in request.Reports)
            {
                var newReport = new GymReport
                {
                    ReportName = reportDto.ReportName,
                    PaymentType = reportDto.PaymentType,
                    TotalAmount = reportDto.TotalAmount,
                    Rows = reportDto.Rows.Select(row => new GymReportRow
                    {
                        CustomerName = row.CustomerName,
                        IsCustomerMember = false,
                        DateOfEnrollment = null,
                        MembershipFee = 0,
                        ContractDuration = "—",
                        MonthlyPayment = 0,
                        IncludePersonalTrainer = false,
                        PersonalTrainerPackage = "—",
                        PaymentOption = "—",
                        DailyPass = row.DailyPass,
                        AmountToPay = row.AmountToPay,
                        OnlinePaymentMethod = string.IsNullOrWhiteSpace(row.OnlinePaymentMethod) ? null : row.OnlinePaymentMethod,
                        ReferenceNumber = string.IsNullOrWhiteSpace(row.ReferenceNumber) ? null : row.ReferenceNumber
                    }).ToList()
                };

                walkInReportsToSave.Add(newReport);
            }

            _context.GymReports.AddRange(walkInReportsToSave);
            await _context.SaveChangesAsync();

            // Clear cache
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new UpdateSavingSuccessfulResponse
            {
                message = $"Successfully saved {walkInReportsToSave.Count} reports.",
                savedIds = walkInReportsToSave.Select(r => r.Id).ToList()
            });
        }

        [HttpPost("non-member-update/{id:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(UpdateWalkInReportResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateNonMemberReport(int id, [FromBody] UpdateWalkInReportRequest request)
        {
            var validator = new UpdateWalkInReportRequestValidator();
            var validatorResult = await validator.ValidateAsync(request);

            if (!validatorResult.IsValid)
            {
                var firstError = validatorResult.Errors.First();
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage));
            }

            var existingReport = await _context.GymReports
                .Include(r => r.Rows)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReport == null)
            {
                return NotFound(new { Message = $"Non-Member Report with ID {id} not found." });
            }

            existingReport.ReportName = request.ReportName;
            existingReport.PaymentType = request.PaymentType;
            existingReport.TotalAmount = request.TotalAmount;

            var incomingRowIds = request.Rows.Where(r => r.Id.HasValue).Select(r => r.Id!.Value).ToList();
            var rowsToDelete = existingReport.Rows.Where(dbRow => !incomingRowIds.Contains(dbRow.Id)).ToList();

            if (rowsToDelete.Any())
            {
                _context.GymReportRows.RemoveRange(rowsToDelete);
            }

            foreach (var incomingRow in request.Rows)
            {
                if (incomingRow.Id.HasValue)
                {
                    var dbRow = existingReport.Rows.FirstOrDefault(r => r.Id == incomingRow.Id.Value);
                    if (dbRow != null)
                    {
                        dbRow.CustomerName = incomingRow.CustomerName;
                        dbRow.DailyPass = incomingRow.DailyPass;
                        dbRow.AmountToPay = incomingRow.AmountToPay;
                        dbRow.OnlinePaymentMethod = string.IsNullOrWhiteSpace(incomingRow.OnlinePaymentMethod) ? null : incomingRow.OnlinePaymentMethod;
                        dbRow.ReferenceNumber = string.IsNullOrWhiteSpace(incomingRow.ReferenceNumber) ? null : incomingRow.ReferenceNumber;
                        dbRow.IsCustomerMember = false;
                        dbRow.DateOfEnrollment = null;
                        dbRow.MembershipFee = 0;
                        dbRow.ContractDuration = "—";
                        dbRow.MonthlyPayment = 0;
                        dbRow.IncludePersonalTrainer = false;
                        dbRow.PersonalTrainerPackage = "—";
                        dbRow.PaymentOption = "—";
                    }
                }
                else
                {
                    var newRow = new GymReportRow
                    {
                        CustomerName = incomingRow.CustomerName,
                        IsCustomerMember = false,
                        DateOfEnrollment = null,
                        MembershipFee = 0,
                        ContractDuration = "—",
                        MonthlyPayment = 0,
                        IncludePersonalTrainer = false,
                        PersonalTrainerPackage = "—",
                        PaymentOption = "—",
                        DailyPass = incomingRow.DailyPass,
                        AmountToPay = incomingRow.AmountToPay,
                        OnlinePaymentMethod = string.IsNullOrWhiteSpace(incomingRow.OnlinePaymentMethod) ? null : incomingRow.OnlinePaymentMethod,
                        ReferenceNumber = string.IsNullOrWhiteSpace(incomingRow.ReferenceNumber) ? null : incomingRow.ReferenceNumber
                    };
                    existingReport.Rows.Add(newRow);
                }
            }

            await _context.SaveChangesAsync();

            // Clear cache
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new SuccessfulResponse { message = "Report successfully updated!" });
        }

        [HttpPost("delete/{id:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var reportToDelete = await _context.GymReports
                .Include(r => r.Rows)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (reportToDelete == null)
            {
                return NotFound(new { Message = $"Report with ID {id} not found." });
            }

            if (reportToDelete.Rows.Any())
            {
                _context.GymReportRows.RemoveRange(reportToDelete.Rows);
            }

            _context.GymReports.Remove(reportToDelete);
            await _context.SaveChangesAsync();

            // Clear cache
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new SuccessfulResponse { message = $"Report {id} successfully deleted!" });
        }

        [HttpPost("rows/update/{rowId:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> UpdateSingleRow(int rowId, [FromBody] UpdateSingleRowRequest request)
        {
            var row = await _context.GymReportRows.FindAsync(rowId);
            if (row == null)
            {
                return NotFound(new ErrorResponse(404, "NOT_FOUND", $"The report row with ID {rowId} was not found."));
            }

            row.CustomerName = request.CustomerName;
            row.IsCustomerMember = request.IsCustomerMember;
            row.DateOfEnrollment = request.DateOfEnrollment;
            row.MembershipFee = request.MembershipFee;
            row.ContractDuration = request.ContractDuration;
            row.MonthlyPayment = request.MonthlyPayment;
            row.DailyPass = request.DailyPass;
            row.IncludePersonalTrainer = request.IncludePersonalTrainer;
            row.PersonalTrainerPackage = request.PersonalTrainerPackage;
            row.PaymentOption = request.PaymentOption;
            row.AmountToPay = request.AmountToPay;
            row.OnlinePaymentMethod = request.OnlinePaymentMethod;
            row.ReferenceNumber = request.ReferenceNumber;

            await _context.SaveChangesAsync();

            await RecalculateReportTotal(row.GymReportId);

            // Clear cache
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new SuccessfulResponse { message = $"Successfully updated row {rowId}!" });
        }

        [HttpPost("rows/delete/{rowId:int}")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(SuccessfulResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> DeleteSingleRow(int rowId)
        {
            var row = await _context.GymReportRows.FindAsync(rowId);

            if (row == null)
            {
                return NotFound(new { Message = $"The report with ID {rowId} not found." });
            }

            int parentReportId = row.GymReportId;

            _context.GymReportRows.Remove(row);
            await _context.SaveChangesAsync();

            await RecalculateReportTotal(parentReportId);

            // Clear cache
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new SuccessfulResponse { message = $"Successfully deleted row {rowId}!" });
        }

        [HttpPost("{reportId:int}/rows/add")]
        [EnableRateLimiting("StrictWritePolicy")]
        [ProducesResponseType(typeof(AddSingleRowResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(typeof(ErrorResponse), StatusCodes.Status404NotFound)]
        public async Task<IActionResult> AddSingleRowToReport(int reportId, [FromBody] AddSingleRowRequest request)
        {
            var parentReport = await _context.GymReports.FindAsync(reportId);
            if (parentReport == null)
            {
                return NotFound(new { Message = $"The master report with ID {reportId} not found." });
            }

            var newRow = new GymReportRow
            {
                GymReportId = reportId,
                CustomerName = request.CustomerName,
                IsCustomerMember = request.IsCustomerMember,
                DateOfEnrollment = request.DateOfEnrollment,
                MembershipFee = request.MembershipFee,
                ContractDuration = request.ContractDuration,
                MonthlyPayment = request.MonthlyPayment,
                DailyPass = request.DailyPass,
                IncludePersonalTrainer = request.IncludePersonalTrainer,
                PersonalTrainerPackage = request.PersonalTrainerPackage,
                PaymentOption = request.PaymentOption,
                AmountToPay = request.AmountToPay,
                OnlinePaymentMethod = request.OnlinePaymentMethod,
                ReferenceNumber = request.ReferenceNumber
            };

            _context.GymReportRows.Add(newRow);
            await _context.SaveChangesAsync();

            await RecalculateReportTotal(reportId);

            // Clear cache
            await EvictReportsCacheAsync(HttpContext.RequestAborted);

            return Ok(new AddSingleRowResponse { message = "Sucessfully added new report!", newRowId = newRow.Id });
        }

        private async Task RecalculateReportTotal(int reportId)
        {
            var report = await _context.GymReports
                .Include(r => r.Rows)
                .FirstOrDefaultAsync(r => r.Id == reportId);

            if (report != null)
            {
                report.TotalAmount = report.Rows.Sum(row => row.AmountToPay);
                report.LastDateUpdated = DateTime.Now;

                await _context.SaveChangesAsync();
            }
        }
    }
}