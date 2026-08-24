namespace FitHolic.DTO
{
    public record UpdateSingleRowRequest(
        string CustomerName,
        bool IsCustomerMember,
        DateOnly? DateOfEnrollment,
        decimal MembershipFee,
        string ContractDuration,
        decimal MonthlyPayment,
        decimal? DailyPass,
        bool IncludePersonalTrainer,
        string? PersonalTrainerPackage,
        string PaymentOption,
        decimal AmountToPay,
        string? OnlinePaymentMethod,
        string? ReferenceNumber
    );

    // DTO para sa pagdagdag ng bagong row sa may umiiral nang Report
    public record AddSingleRowRequest(
        string CustomerName,
        bool IsCustomerMember,
        DateOnly? DateOfEnrollment,
        decimal MembershipFee,
        string ContractDuration,
        decimal MonthlyPayment,
        decimal? DailyPass,
        bool IncludePersonalTrainer,
        string? PersonalTrainerPackage,
        string PaymentOption,
        decimal AmountToPay,
        string? OnlinePaymentMethod,
        string? ReferenceNumber
    );

    public class AddSingleRowResponse
    {
        public string message { get; set; }
        public int newRowId  { get; set; }
    }
}
