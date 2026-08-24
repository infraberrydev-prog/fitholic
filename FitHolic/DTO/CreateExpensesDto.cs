using FluentValidation;

namespace FitHolic.DTO
{
    public record CreateExpensesRequest(
        string ReportName,
        decimal CashPayment,
        decimal? OnlinePayment, // Optional sa UI
        decimal Expenses,
        decimal Payroll,
        decimal Electricity,
        decimal TotalExpenses
    );

    public class CreateExpensesRequestValidator : AbstractValidator<CreateExpensesRequest>
    {
        public CreateExpensesRequestValidator()
        {
            ClassLevelCascadeMode = CascadeMode.Stop;

            RuleFor(x => x.ReportName)
                .NotEmpty().WithErrorCode("EX001").WithMessage("{PropertyName} is required.");

            RuleFor(x => x.CashPayment)
                .GreaterThanOrEqualTo(0).WithErrorCode("EX002").WithMessage("{PropertyName} must be a valid amount.");

            RuleFor(x => x.OnlinePayment)
                .GreaterThanOrEqualTo(0).WithErrorCode("EX003").WithMessage("{PropertyName} must be a valid amount.");

            RuleFor(x => x.Expenses)
                .GreaterThanOrEqualTo(0).WithErrorCode("EX004").WithMessage("{PropertyName} must be a valid amount.");

            RuleFor(x => x.Payroll)
                .GreaterThanOrEqualTo(0).WithErrorCode("EX005").WithMessage("{PropertyName} must be a valid amount.");

            RuleFor(x => x.Electricity)
                .GreaterThanOrEqualTo(0).WithErrorCode("EX006").WithMessage("{PropertyName} must be a valid amount.");

            RuleFor(x => x.TotalExpenses)
                .GreaterThanOrEqualTo(0).WithErrorCode("EX006").WithMessage("{PropertyName} must be a valid amount.");
        }
    }
}