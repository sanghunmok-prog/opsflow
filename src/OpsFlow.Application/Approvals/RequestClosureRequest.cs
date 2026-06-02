namespace OpsFlow.Application.Approvals;

public sealed record RequestClosureRequest(
    string? RequestReason,
    string? RowVersion);
