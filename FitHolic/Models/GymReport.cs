using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FitHolic.Models
{
    public class GymReport
    {
        public int Id { get; set; }

        [Required]
        public string ReportName { get; set; } = string.Empty;

        [Required]
        public string PaymentType { get; set; } = "Cash";

        public decimal TotalAmount { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public List<GymReportRow> Rows { get; set; } = new();
    }

    public class GymReportRow
    {
        public int Id { get; set; }

        public int GymReportId { get; set; }
        [ForeignKey(nameof(GymReportId))]
        public GymReport? GymReport { get; set; }

        // Membership Details
        [Required]
        public string CustomerName { get; set; } = string.Empty;
        public bool IsCustomerMember { get; set; }
        public DateOnly? DateOfEnrollment { get; set; }
        public decimal MembershipFee { get; set; }
        public string ContractDuration { get; set; } = string.Empty; // e.g., "6 Months"
        public decimal MonthlyPayment { get; set; }

        // Personal Trainer Setup
        public bool IncludePersonalTrainer { get; set; }
        public string? PersonalTrainerPackage { get; set; } // e.g., "10 Sessions"
        public string PaymentOption { get; set; } = "Pay in Full"; // Pay in Full o Partial
        public decimal AmountToPay { get; set; }
        public decimal? DailyPass { get; set; }
        public string? OnlinePaymentMethod { get; set; } // e.g., "Credit Card", "GCash"
        public string? ReferenceNumber { get; set; }
    }
}
