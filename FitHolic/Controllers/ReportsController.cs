using ClosedXML.Excel;
using DocumentFormat.OpenXml.Vml.Spreadsheet;
using FitHolic.DTO;
using FitHolic.Models;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.MicrosoftExtensions;

namespace FitHolic.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ReportsController : ControllerBase
    {
        private readonly FitHolicDbContext _context;

        public ReportsController(FitHolicDbContext context)
        {
            _context = context;
        }

        [HttpPost("save-report")]
        public async Task<IActionResult> CreateBulkReports([FromBody] BulkCreateReportRequest request)
        {
            var validator = new BulkCreateReportRequestValidator();
            var validatorResult = await validator.ValidateAsync(request);

            if (!validatorResult.IsValid)
            {
                var firstError = validatorResult.Errors.First();
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage));
            }

            var gymReportsToSave = new List<GymReport>();

            foreach (var reportDto in request.Reports)
            {
                var newReport = new GymReport
                {
                    ReportName = reportDto.ReportName,
                    PaymentType = reportDto.PaymentType,
                    TotalAmount = reportDto.TotalAmount,
                    Rows = reportDto.Rows.Select(row =>
                    {
                        DateOnly? finalEnrollmentDate = null;
                        decimal finalMembershipFee = row.MembershipFee;

                        if (row.IsCustomerMember)
                        {
                            finalEnrollmentDate = null;
                            finalMembershipFee = 0;
                        }
                        else
                        {
                            finalEnrollmentDate = row.DateOfEnrollment;
                            finalMembershipFee = row.MembershipFee;
                        }

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
                            OnlinePaymentMethod = string.IsNullOrWhiteSpace(row.OnlinePaymentMethod) ? null : row.OnlinePaymentMethod,
                            ReferenceNumber = string.IsNullOrWhiteSpace(row.ReferenceNumber) ? null : row.ReferenceNumber
                        };
                    }).ToList()
                };

                gymReportsToSave.Add(newReport);
            }

            _context.GymReports.AddRange(gymReportsToSave);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                Message = $"Successfully saved {gymReportsToSave.Count} reports.",
                SavedIds = gymReportsToSave.Select(r => r.Id)
            });
        }

        [HttpGet("get-all-reports")]
        public async Task<ActionResult<PagedReportResponse>> GetReports(
            [FromQuery] string? search,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 10;

            var query = _context.GymReports.Include(r => r.Rows).AsQueryable();

            if (!string.IsNullOrWhiteSpace(search))
            {
                query = query.Where(r => r.ReportName.ToLower().Contains(search.ToLower()));
            }

            int totalRecords = await query.CountAsync();

            var reports = await query
                .OrderByDescending(r => r.CreatedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)              
                .ToListAsync();

            var reportList = reports.Select(r => new ReportListDto(
                ReportId: $"MR{r.Id:D5}",
                ReportName: r.ReportName,
                ExpectedCashRevenue: r.Rows.Where(row => string.IsNullOrWhiteSpace(row.OnlinePaymentMethod)).Sum(row => row.AmountToPay),
                ExpectedOnlineRevenue: r.Rows.Where(row => !string.IsNullOrWhiteSpace(row.OnlinePaymentMethod)).Sum(row => row.AmountToPay),
                DateTimeCreated: r.CreatedAt
            )).ToList();

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

        [HttpGet("{id}/details")]
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

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> DeleteReport(int id)
        {
            var report = await _context.GymReports.FindAsync(id);

            if (report == null)
            {
                return NotFound(new { Message = $"No Report with ID {id} found." });
            }

            _context.GymReports.Remove(report);

            await _context.SaveChangesAsync();

            return Ok(new { Message = $"Succesfully deleted Report {id}." });
        }

        [HttpPost("update/{id:int}")]
        public async Task<IActionResult> UpdateReport(int id, [FromBody] UpdateSingleReportRequest request)
        {
            var validator = new UpdateReportRequestValidator();
            var validatorResult = await validator.ValidateAsync(request);

            if (!validatorResult.IsValid)
            {
                var firstError = validatorResult.Errors.First();
                return BadRequest(new ErrorResponse(
                    statusCode: 400,
                    errorType: firstError.ErrorCode,
                    message: firstError.ErrorMessage
                    ));
            }

            var existingReport = await _context.GymReports
                .Include(r => r.Rows)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (existingReport == null)
            {
                return NotFound(new { Message = $"No Report with ID {id} found." });
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
                DateOnly? finalEnrollmentDate = null;
                decimal finalMembershipFee = incomingRow.MembershipFee;

                if (incomingRow.IsCustomerMember)
                {
                    finalEnrollmentDate = null;
                    finalMembershipFee = 0;
                }
                else
                {
                    finalEnrollmentDate = incomingRow.DateOfEnrollment;
                    finalMembershipFee = incomingRow.MembershipFee;
                }

                if (incomingRow.Id.HasValue)
                {
                    var dbRow = existingReport.Rows.FirstOrDefault(r => r.Id == incomingRow.Id.Value);
                    if (dbRow != null)
                    {
                        dbRow.CustomerName = incomingRow.CustomerName;
                        dbRow.IsCustomerMember = incomingRow.IsCustomerMember;
                        dbRow.DateOfEnrollment = finalEnrollmentDate;
                        dbRow.MembershipFee = finalMembershipFee;

                        dbRow.DailyPass = null;
                        dbRow.ContractDuration = string.IsNullOrWhiteSpace(incomingRow.ContractDuration) ? "—" : incomingRow.ContractDuration;
                        dbRow.MonthlyPayment = incomingRow.MonthlyPayment;
                        dbRow.IncludePersonalTrainer = incomingRow.IncludePersonalTrainer;
                        dbRow.PersonalTrainerPackage = incomingRow.IncludePersonalTrainer ? incomingRow.PersonalTrainerPackage : "—";
                        dbRow.PaymentOption = incomingRow.PaymentOption;
                        dbRow.AmountToPay = incomingRow.AmountToPay;
                        dbRow.OnlinePaymentMethod = string.IsNullOrWhiteSpace(incomingRow.OnlinePaymentMethod) ? null : incomingRow.OnlinePaymentMethod;
                        dbRow.ReferenceNumber = string.IsNullOrWhiteSpace(incomingRow.ReferenceNumber) ? null : incomingRow.ReferenceNumber;
                    }
                }
                else
                {
                    var newRow = new GymReportRow
                    {
                        CustomerName = incomingRow.CustomerName,
                        IsCustomerMember = incomingRow.IsCustomerMember,
                        DateOfEnrollment = finalEnrollmentDate,
                        MembershipFee = finalMembershipFee,

                        DailyPass = null,
                        ContractDuration = string.IsNullOrWhiteSpace(incomingRow.ContractDuration) ? "—" : incomingRow.ContractDuration,
                        MonthlyPayment = incomingRow.MonthlyPayment,
                        IncludePersonalTrainer = incomingRow.IncludePersonalTrainer,
                        PersonalTrainerPackage = incomingRow.IncludePersonalTrainer ? incomingRow.PersonalTrainerPackage : "—",
                        PaymentOption = incomingRow.PaymentOption,
                        AmountToPay = incomingRow.AmountToPay,
                        OnlinePaymentMethod = string.IsNullOrWhiteSpace(incomingRow.OnlinePaymentMethod) ? null : incomingRow.OnlinePaymentMethod,
                        ReferenceNumber = string.IsNullOrWhiteSpace(incomingRow.ReferenceNumber) ? null : incomingRow.ReferenceNumber
                    };
                    existingReport.Rows.Add(newRow);
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { Message = "Report successfully updated!" });
        }

        [HttpGet("download")]
        public async Task<IActionResult> DownloadMembershipReports()
        {
            var reports = await _context.GymReports
                .Include(r => r.Rows)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            using (var workbook = new XLWorkbook())
            {
                var worksheet = workbook.Worksheets.Add("Membership Reports");

                worksheet.Cell(1, 1).Value = "Report ID";
                worksheet.Cell(1, 2).Value = "Report Name";
                worksheet.Cell(1, 3).Value = "Expected Cash Revenue";
                worksheet.Cell(1, 4).Value = "Expected Online Revenue";
                worksheet.Cell(1, 5).Value = "Date & Time Created";

                var headerRow = worksheet.Row(1);
                headerRow.Style.Font.Bold = true;
                headerRow.Style.Fill.BackgroundColor = XLColor.FromHtml("#2E7D32");
                headerRow.Style.Font.FontColor = XLColor.White;

                int currentRow = 2;
                foreach (var report in reports)
                {
                    decimal cashRevenue = report.Rows
                        .Where(row => string.IsNullOrEmpty(row.OnlinePaymentMethod))
                        .Sum(row => row.AmountToPay);

                    decimal onlineRevenue = report.Rows
                        .Where(row => !string.IsNullOrEmpty(row.OnlinePaymentMethod))
                        .Sum(row => row.AmountToPay);

                    worksheet.Cell(currentRow, 1).Value = $"MR{report.Id:D5}";
                    worksheet.Cell(currentRow, 2).Value = report.ReportName;

                    worksheet.Cell(currentRow, 3).Value = cashRevenue;
                    worksheet.Cell(currentRow, 3).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 4).Value = onlineRevenue;
                    worksheet.Cell(currentRow, 4).Style.NumberFormat.Format = "₱#,##0.00";

                    worksheet.Cell(currentRow, 5).Value = report.CreatedAt.ToString("yyyy-MM-dd HH:mm:ss");

                    currentRow++;
                }

                worksheet.Columns().AdjustToContents();

                using (var stream = new MemoryStream())
                {
                    workbook.SaveAs(stream);
                    var content = stream.ToArray();

                    string fileName = $"FitHolic_Membership_Reports_{DateTime.Now:yyyyMMdd}.xlsx";

                    return File(
                        content,
                        "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                        fileName
                    );
                }
            }
        }

        [HttpPost("save-non-member-report")]
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

            return Ok(new
            {
                Message = $"Successfully saved {walkInReportsToSave.Count} reports.",
                SavedIds = walkInReportsToSave.Select(r => r.Id)
            });
        }

        [HttpPost("non-member-update/{id:int}")]
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

            return Ok(new { Message = "Report successfully update!" });
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteReportNonMember(int id)
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

            return Ok(new { Message = $"Report successfully deleted!" });
        }
    }
}
