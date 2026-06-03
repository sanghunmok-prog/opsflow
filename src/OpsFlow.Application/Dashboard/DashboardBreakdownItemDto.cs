namespace OpsFlow.Application.Dashboard;

public sealed record DashboardBreakdownItemDto(
    string Key,
    string Label,
    int Count,
    IReadOnlyDictionary<string, string>? RouteQuery = null);
