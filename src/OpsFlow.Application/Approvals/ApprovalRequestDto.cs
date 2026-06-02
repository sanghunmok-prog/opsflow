using OpsFlow.Application.Cases;

namespace OpsFlow.Application.Approvals;

public sealed record ApprovalRequestDto(
    Guid Id,
    Guid CaseId,
    string CaseNumber,
    string CaseTitle,
    string Priority,
    string CaseStatus,
    string ApprovalStatus,
    string RequestReason,
    UserSummaryDto RequestedBy,
    DateTime RequestedAtUtc,
    string RowVersion);
