namespace OpsFlow.Application.Dashboard;

public sealed record DashboardBreakdownsDto(
    IReadOnlyList<DashboardBreakdownItemDto> ByStatus,
    IReadOnlyList<DashboardBreakdownItemDto> ByPriority,
    IReadOnlyList<DashboardBreakdownItemDto> ByCaseType,
    IReadOnlyList<DashboardBreakdownItemDto> ByAssignee);
