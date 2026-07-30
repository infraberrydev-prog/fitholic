namespace FitHolic.Models
{
    public class ContractDurationDto
    {
        public int Id { get; set; }
        public string ContractDuration { get; set; }
        public decimal MonthlyPayment { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}
