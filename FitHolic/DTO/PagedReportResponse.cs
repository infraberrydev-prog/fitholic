namespace FitHolic.DTO
{
    public record PagedReportResponse(
        List<ReportListDto> Data,
        int TotalRecords,
        int CurrentPage,
        int TotalPages,
        int PageSize
    );

    public record ReportListDto(
        int Id,
        string ReportId,
        string ReportName,
        decimal ExpectedCashRevenue,
        decimal ExpectedOnlineRevenue,
        DateTime DateTimeCreated,
        DateTime? LastDateUpdated
    );
}
