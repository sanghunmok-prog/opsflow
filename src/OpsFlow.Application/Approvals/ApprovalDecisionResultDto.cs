using OpsFlow.Application.Cases;

namespace OpsFlow.Application.Approvals;

public sealed record ApprovalDecisionResultDto(
    Guid ApprovalId,
    Guid CaseId,
    string CaseNumber,
    string CaseTitle,
    string ApprovalStatus,
    string CaseStatus,
    string? DecisionReason,
    UserSummaryDto ReviewedBy,
    DateTime DecisionAtUtc,
    string RowVersion);
