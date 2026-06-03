namespace OpsFlow.Application.Dashboard;

public sealed record DashboardSummaryDto(
    int OpenCases,
    int OverdueOpenCases,
    int PendingApprovals,
    double AverageOpenAgeHours,
    double SlaBreachRate);
