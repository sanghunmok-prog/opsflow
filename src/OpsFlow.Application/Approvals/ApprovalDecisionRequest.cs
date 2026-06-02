namespace OpsFlow.Application.Approvals;

public sealed record ApprovalDecisionRequest(
    string? DecisionReason,
    string? RowVersion = null);
