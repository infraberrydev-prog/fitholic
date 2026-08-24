using FluentValidation;

namespace FitHolic.DTO
{
    public record UpdateWalkInReportRequest(
        string ReportName,
        string PaymentType,
        decimal TotalAmount,
        List<UpdateWalkInRowDto> Rows
    );

    public record UpdateWalkInRowDto(
        int? Id,
        string CustomerName,
        decimal DailyPass,
        decimal AmountToPay,
        string? OnlinePaymentMethod,
        string? ReferenceNumber
    );

    public class UpdateWalkInReportResponse
    {
        public string message { get; set; } = string.Empty;
    }

    public class UpdateWalkInReportRequestValidator : AbstractValidator<UpdateWalkInReportRequest>
    {
        public UpdateWalkInReportRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ReportName)
                .NotEmpty().WithErrorCode("RE001").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.Rows)
                .NotEmpty().WithErrorCode("RE002").WithMessage("Report must contain at least one row.")
                .ForEach(row => row.SetValidator(new UpdateWalkInRowDtoValidator()));
        }
    }

    public class UpdateWalkInRowDtoValidator : AbstractValidator<UpdateWalkInRowDto>
    {
        public UpdateWalkInRowDtoValidator()
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
