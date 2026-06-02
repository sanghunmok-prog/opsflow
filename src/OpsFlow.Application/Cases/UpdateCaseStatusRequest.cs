namespace OpsFlow.Application.Cases;

public sealed record UpdateCaseStatusRequest(
    string? TargetStatus,
    string? Reason,
    string? RowVersion);
