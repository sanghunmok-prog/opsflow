namespace OpsFlow.Application.Dashboard;

public interface IDashboardQueryService
{
    Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default);

    Task<DashboardBreakdownsDto> GetBreakdownsAsync(CancellationToken cancellationToken = default);
}
