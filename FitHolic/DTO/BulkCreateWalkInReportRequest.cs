using FluentValidation;

namespace FitHolic.DTO
{
    public record BulkCreateWalkInReportRequest(
        List<CreateWalkInReportMasterDto> Reports
    );

    public record CreateWalkInReportMasterDto(
        string ReportName,
        string PaymentType,
        decimal TotalAmount,
        List<CreateWalkInRowDto> Rows
    );

    public record CreateWalkInRowDto(
        string CustomerName,
        decimal DailyPass,
        decimal AmountToPay,
        string? OnlinePaymentMethod,
        string? ReferenceNumber
    );

    public class BulkCreateCreateReportResponse
    {
        public string message { get; set; } = string.Empty;
        public List<int> savedIds { get; set; } = new();
    }

    public class BulkCreateWalkInReportRequestValidator : AbstractValidator<BulkCreateWalkInReportRequest>
    {
        public BulkCreateWalkInReportRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Reports)
                .NotEmpty().WithErrorCode("RE000").WithMessage("Reports list cannot be empty.")
                .ForEach(report => report.SetValidator(new CreateWalkInReportMasterDtoValidator()));
        }
    }

    public class CreateWalkInReportMasterDtoValidator : AbstractValidator<CreateWalkInReportMasterDto>
    {
        public CreateWalkInReportMasterDtoValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ReportName)
                .NotEmpty().WithErrorCode("RE001").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.Rows)
                .NotEmpty().WithErrorCode("RE002").WithMessage("Report must contain at least one row.")
                .ForEach(row => row.SetValidator(new CreateWalkInReportRowValidator()));
        }
    }

    public class CreateWalkInReportRowValidator : AbstractValidator<CreateWalkInRowDto>
    {
        public CreateWalkInReportRowValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithErrorCode("RE001").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.DailyPass)
                .NotEmpty().WithErrorCode("RE002").WithMessage("{PropertyName} is required.")
                .GreaterThanOrEqualTo(0).WithErrorCode("RE003").WithMessage("{PropertyName} must be a valid amount.");

            RuleFor(x => x.OnlinePaymentMethod)
               .NotEmpty().WithErrorCode("RE004").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.AmountToPay)
               .GreaterThanOrEqualTo(0).WithErrorCode("RE005").WithMessage("{PropertyName} must be a valid amount.");
        }
    }
}
