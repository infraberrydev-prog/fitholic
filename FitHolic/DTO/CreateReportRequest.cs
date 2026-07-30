using FluentValidation;
using System;
using System.Collections.Generic;

namespace FitHolic.DTO
{
    public record BulkCreateReportRequest(
        List<CreateReportRequest> Reports
    );

    public record CreateReportRequest(
        string ReportName,
        string PaymentType, // "Cash" o "Online"
        decimal TotalAmount,
        List<ReportRowDto> Rows
    );

    public record ReportRowDto(
        string CustomerName,
        bool IsCustomerMember,
        DateOnly? DateOfEnrollment,
        decimal? DailyPass,
        decimal MembershipFee,
        string ContractDuration,
        decimal MonthlyPayment,
        bool IncludePersonalTrainer,
        string? PersonalTrainerPackage,
        string PaymentOption,
        decimal AmountToPay,
        string? OnlinePaymentMethod,
        string? ReferenceNumber
    );

    public class BulkCreateReportRequestValidator : AbstractValidator<BulkCreateReportRequest>
    {
        public BulkCreateReportRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Reports)
                .NotEmpty().WithErrorCode("RE000").WithMessage("Reports list cannot be empty.")
                .ForEach(report => report.SetValidator(new CreateReportRequestValidator()));
        }
    }

    public class CreateReportRequestValidator : AbstractValidator<CreateReportRequest>
    {
        public CreateReportRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ReportName)
                .NotEmpty().WithErrorCode("RE001").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.Rows)
                .NotEmpty().WithErrorCode("RE002").WithMessage("Report must contain at least one row.")
                .ForEach(row => row.SetValidator(new CreateReportRowValidator()));
        }
    }

    public class CreateReportRowValidator : AbstractValidator<ReportRowDto>
    {
        public CreateReportRowValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.CustomerName)
                .NotEmpty().WithErrorCode("RE003").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.IsCustomerMember)
                .NotNull().WithErrorCode("RE004").WithMessage("{PropertyName} specification is required.");

            When(x => !x.IsCustomerMember, () =>
            {
                RuleFor(x => x.DateOfEnrollment)
                    .NotEmpty().WithErrorCode("RE005").WithMessage("{PropertyName} is required when customer is not a member yet.");

                RuleFor(x => x.MembershipFee)
                    .NotEmpty().WithErrorCode("RE006").WithMessage("{PropertyName} is required when customer is not a member yet.")
                    .GreaterThan(0).WithErrorCode("RE006_A").WithMessage("{PropertyName} must be greater than 0.");
            });

            RuleFor(x => x.ContractDuration)
                .NotEmpty().WithErrorCode("RE007").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.MonthlyPayment)
               .GreaterThanOrEqualTo(0).WithErrorCode("RE008").WithMessage("{PropertyName} must be a valid amount.");

            When(x => x.IncludePersonalTrainer, () =>
            {
                RuleFor(x => x.PersonalTrainerPackage)
                    .NotEmpty().WithErrorCode("RE009").WithMessage("{PropertyName} is required when customer has Personal Trainer.");
            });

            RuleFor(x => x.PaymentOption)
               .NotEmpty().WithErrorCode("RE010").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.AmountToPay)
               .GreaterThanOrEqualTo(0).WithErrorCode("RE011").WithMessage("{PropertyName} must be a valid amount.");
        }
    }
}