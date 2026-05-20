using OpsFlow.Api.Data.Entities;

namespace OpsFlow.Api.Data.Seed;

public sealed record SeedDataSet(
    IReadOnlyList<AppUser> Users,
    IReadOnlyList<CaseType> CaseTypes,
    IReadOnlyList<SlaRule> SlaRules,
    IReadOnlyList<OpsCase> Cases,
    IReadOnlyList<CaseNote> CaseNotes,
    IReadOnlyList<StatusHistory> StatusHistories,
    IReadOnlyList<AssignmentHistory> AssignmentHistories,
    IReadOnlyList<ApprovalRequest> ApprovalRequests,
    IReadOnlyList<AuditLog> AuditLogs);
