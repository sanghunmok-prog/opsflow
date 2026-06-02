using OpsFlow.Application.Approvals;

namespace OpsFlow.Application.Cases;

public sealed record CaseDetailDto(
    Guid Id,
    string CaseNumber,
    string Title,
    string Description,
    CaseTypeSummaryDto CaseType,
    string Priority,
    string Status,
    UserSummaryDto? AssignedTo,
    UserSummaryDto CreatedBy,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc,
    DateTime DueAtUtc,
    DateTime? ClosedAtUtc,
    bool IsOverdue,
    string RowVersion,
    ApprovalSummaryDto? ApprovalSummary = null);
