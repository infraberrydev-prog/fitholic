namespace FitHolic.Models
{
    public class GymExpensesReport
    {
        public int Id { get; set; }
        public string ReportName { get; set; } = null!;
        public decimal CashPayment { get; set; }
        public decimal? OnlinePayment { get; set; }
        public decimal Expenses { get; set; }
        public decimal Payroll { get; set; }
        public decimal Electricity { get; set; }
        public decimal TotalExpenses { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public DateTime? LastDateUpdated { get; set; }
    }
}
