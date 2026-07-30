namespace FitHolic.DTO
{
    public record ExpensesPagedResponse(
        int TotalCount,
        int TotalPages,
        int CurrentPage,
        int PageSize,
        List<ExpensesListDto> Data
    );

    public record ExpensesListDto(
        int Id,
        string FormattedReportId,
        string ReportName,
        decimal TotalExpenses,
        DateTime DateCreated,
        DateTime? LastUpdated
    );
}
