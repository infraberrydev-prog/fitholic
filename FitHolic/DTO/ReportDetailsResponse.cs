namespace FitHolic.DTO
{
    public record ReportDetailsResponse(
        string ReportId,
        string ReportName,
        string PaymentType,
        decimal TotalAmount,
        DateTime DateCreated,
        List<ReportRowDetailsDto> CashRows,
        List<ReportRowDetailsDto> OnlineRows
    );

    public record ReportRowDetailsDto(
        int Id,
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
}
