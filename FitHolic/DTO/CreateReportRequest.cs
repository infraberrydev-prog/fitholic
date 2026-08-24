using FluentValidation;
using System;
using System.Collections.Generic;

namespace FitHolic.DTO
{
    // ==========================================
    // 1. REVISED DTOs (GROUPED BY CASH/ONLINE)
    // ==========================================

    public record GroupedBulkCreateReportRequest(
        List<GroupedCreateReportRequest> Reports
    );

    public record GroupedCreateReportRequest(
        string ReportName,
        string PaymentType,
        decimal TotalAmount,
        List<ReportRowDto> CashRows,
        List<ReportRowDto> OnlineRows
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

    // ==========================================
    // 2. VALIDATORS
    // ==========================================

    public class GroupedBulkCreateReportRequestValidator : AbstractValidator<GroupedBulkCreateReportRequest>
    {
        public GroupedBulkCreateReportRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.Reports)
                .NotEmpty().WithErrorCode("RE000").WithMessage("Reports list cannot be empty.")
                .ForEach(report => report.SetValidator(new GroupedCreateReportRequestValidator()));
        }
    }

    public class GroupedCreateReportRequestValidator : AbstractValidator<GroupedCreateReportRequest>
    {
        public GroupedCreateReportRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ReportName)
                .NotEmpty().WithErrorCode("RE001").WithMessage("{PropertyName} is required.");

            // Dapat may laman ang kahit isa man lang sa CashRows o OnlineRows
            RuleFor(x => x)
                .Must(x => (x.CashRows != null && x.CashRows.Count > 0) || (x.OnlineRows != null && x.OnlineRows.Count > 0))
                .WithErrorCode("RE002")
                .WithMessage("Report must contain at least one row in Cash or Online section.");

            // Validate Cash Rows
            RuleFor(x => x.CashRows)
                .ForEach(row => row.SetValidator(new CreateReportRowValidator(isOnlineRow: false)));

            // Validate Online Rows
            RuleFor(x => x.OnlineRows)
                .ForEach(row => row.SetValidator(new CreateReportRowValidator(isOnlineRow: true)));
        }
    }

    public class CreateReportRowValidator : AbstractValidator<ReportRowDto>
    {
        public CreateReportRowValidator(bool isOnlineRow = false)
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

            // Validation na epektibo lang kapag Online Row ang tinitingnan
            if (isOnlineRow)
            {
                RuleFor(x => x.OnlinePaymentMethod)
                    .NotEmpty().WithErrorCode("RE012").WithMessage("{PropertyName} (e.g. GCash, Maya) is required for online payments.");

                RuleFor(x => x.ReferenceNumber)
                    .NotEmpty().WithErrorCode("RE013").WithMessage("{PropertyName} is required for online payments.");
            }
        }
    }
}