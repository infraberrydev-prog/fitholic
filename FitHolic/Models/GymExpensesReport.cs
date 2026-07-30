namespace FitHolic.Models
{
    public class GymExpensesReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = null!;
        public decimal Payroll { get; set; }
        public decimal? OnlinePayment { get; set; }
        public decimal Electricity { get; set; }
        public decimal Water { get; set; }
        public decimal OtherUtilities { get; set; }
        public decimal TotalExpenses { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
