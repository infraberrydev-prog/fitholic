namespace FitHolic.DTO
{
    public record ReportSummaryResponse(
        string ReportId,
        string ReportName,
        decimal ExpectedCashRevenue,
        decimal ExpectedOnlineRevenue,
        DateTime DateQueryCreated,
        DateTime? LastUpdated
    );
}
