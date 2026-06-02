using OpsFlow.Application.Cases;

namespace OpsFlow.Application.Approvals;

public sealed record ApprovalQueueItemDto(
    Guid Id,
    Guid CaseId,
    string CaseNumber,
    string CaseTitle,
    string Priority,
    string CaseStatus,
    string RequestReason,
    UserSummaryDto RequestedBy,
    DateTime RequestedAtUtc,
    UserSummaryDto? AssignedTo,
    DateTime DueAtUtc,
    bool IsOverdue,
    string RowVersion);
