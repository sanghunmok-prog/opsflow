using OpsFlow.Application.Cases;

namespace OpsFlow.Application.Approvals;

public sealed record ApprovalSummaryDto(
    Guid ApprovalId,
    string Status,
    string RequestReason,
    UserSummaryDto RequestedBy,
    DateTime RequestedAtUtc,
    string? DecisionReason,
    UserSummaryDto? ReviewedBy,
    DateTime? DecisionAtUtc);
