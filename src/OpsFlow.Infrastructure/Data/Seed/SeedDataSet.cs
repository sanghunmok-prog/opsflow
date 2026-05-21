using Microsoft.AspNetCore.Identity;
using OpsFlow.Domain.Entities;

namespace OpsFlow.Infrastructure.Data.Seed;

public sealed record SeedDataSet(
    IReadOnlyList<IdentityRole<Guid>> Roles,
    IReadOnlyList<AppUser> Users,
    IReadOnlyList<IdentityUserRole<Guid>> UserRoles,
    IReadOnlyList<CaseType> CaseTypes,
    IReadOnlyList<SlaRule> SlaRules,
    IReadOnlyList<OpsCase> Cases,
    IReadOnlyList<CaseNote> CaseNotes,
    IReadOnlyList<StatusHistory> StatusHistories,
    IReadOnlyList<AssignmentHistory> AssignmentHistories,
    IReadOnlyList<ApprovalRequest> ApprovalRequests,
    IReadOnlyList<AuditLog> AuditLogs);
