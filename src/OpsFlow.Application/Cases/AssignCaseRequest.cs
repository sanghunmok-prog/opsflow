namespace OpsFlow.Application.Cases;

public sealed record AssignCaseRequest(
    Guid? AssignedToUserId,
    string? Reason,
    string? RowVersion = null);
