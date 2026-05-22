using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Identity;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;

namespace OpsFlow.Infrastructure.Data.Seed;

public static class SeedDataGenerator
{
    public const int CaseCount = 320;
    public const string DemoPassword = "Password123!";

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static readonly (string Email, string DisplayName, string Role)[] UserSeeds =
    [
        ("admin@opsflow.local", "Admin User", OpsFlowRoles.Admin),
        ("manager@opsflow.local", "Morgan Manager", OpsFlowRoles.Manager),
        ("analyst1@opsflow.local", "Alex Analyst", OpsFlowRoles.Analyst),
        ("analyst2@opsflow.local", "Blair Analyst", OpsFlowRoles.Analyst),
        ("analyst3@opsflow.local", "Casey Analyst", OpsFlowRoles.Analyst)
    ];

    private static readonly (string Name, string Description)[] CaseTypeSeeds =
    [
        ("Invoice Mismatch", "Invoice totals or terms do not match internal records."),
        ("Delayed Shipment", "A shipment is late or missing an operational update."),
        ("Missing Document", "Required paperwork is incomplete or unavailable."),
        ("Customer Escalation", "A customer-facing issue requires operations follow-up."),
        ("Duplicate Account", "Potential duplicate account data requires review."),
        ("Vendor Approval Issue", "A vendor approval step is blocked or incomplete.")
    ];

    private static readonly CasePriority[] Priorities =
    [
        CasePriority.Critical,
        CasePriority.High,
        CasePriority.Medium,
        CasePriority.Low
    ];

    private static readonly CaseStatus[] Statuses =
    [
        CaseStatus.New,
        CaseStatus.Assigned,
        CaseStatus.InReview,
        CaseStatus.WaitingInfo,
        CaseStatus.Resolved,
        CaseStatus.Closed,
        CaseStatus.Reopened
    ];

    public static SeedDataSet Generate(DateTime nowUtc)
    {
        nowUtc = DateTime.SpecifyKind(nowUtc, DateTimeKind.Utc);
        var baselineUtc = new DateTime(nowUtc.Year, nowUtc.Month, nowUtc.Day, 9, 0, 0, DateTimeKind.Utc);

        var roles = CreateRoles();
        var users = CreateUsers(baselineUtc);
        var userRoles = CreateUserRoles(users, roles);
        var usersByEmail = users.ToDictionary(x => x.Email!);
        var analystRoleId = roles.Single(x => x.Name == OpsFlowRoles.Analyst).Id;
        var analystUserIds = userRoles.Where(x => x.RoleId == analystRoleId).Select(x => x.UserId).ToHashSet();
        var analysts = users.Where(x => analystUserIds.Contains(x.Id)).ToArray();
        var manager = usersByEmail["manager@opsflow.local"];
        var admin = usersByEmail["admin@opsflow.local"];

        var caseTypes = CreateCaseTypes(baselineUtc);
        var slaRules = CreateSlaRules(caseTypes, baselineUtc);
        var cases = new List<OpsCase>(CaseCount);
        var notes = new List<CaseNote>();
        var statusHistories = new List<StatusHistory>();
        var assignmentHistories = new List<AssignmentHistory>();
        var approvalRequests = new List<ApprovalRequest>();
        var auditLogs = new List<AuditLog>();

        for (var index = 1; index <= CaseCount; index++)
        {
            var caseType = caseTypes[(index - 1) % caseTypes.Count];
            var priority = Priorities[(index + 1) % Priorities.Length];
            var status = Statuses[index % Statuses.Length];
            var createdAtUtc = baselineUtc.AddHours(-((index % 30) * 8 + index / 7));
            var assignedTo = status == CaseStatus.New ? null : analysts[index % analysts.Length];
            var targetHours = TargetHours(priority);
            var dueAtUtc = createdAtUtc.AddHours(targetHours);

            if (index <= 45)
            {
                status = index % 3 == 0 ? CaseStatus.WaitingInfo : CaseStatus.InReview;
                priority = index % 2 == 0 ? CasePriority.High : CasePriority.Medium;
                assignedTo = analysts[index % analysts.Length];
                createdAtUtc = baselineUtc.AddDays(-8).AddHours(index % 12);
                dueAtUtc = baselineUtc.AddHours(-index);
            }

            if (index is >= 46 and <= 60)
            {
                priority = index % 2 == 0 ? CasePriority.Critical : CasePriority.High;
                status = CaseStatus.Resolved;
                assignedTo = analysts[index % analysts.Length];
                createdAtUtc = baselineUtc.AddDays(-3).AddHours(index % 8);
                dueAtUtc = createdAtUtc.AddHours(TargetHours(priority));
            }

            if (status == CaseStatus.Closed)
            {
                assignedTo ??= analysts[index % analysts.Length];
            }

            targetHours = TargetHours(priority);
            var resolvedAtUtc = status is CaseStatus.Resolved or CaseStatus.PendingApproval or CaseStatus.Closed
                ? createdAtUtc.AddHours(Math.Min(targetHours + 4, 96))
                : (DateTime?)null;
            var closedAtUtc = status == CaseStatus.Closed
                ? resolvedAtUtc?.AddHours(8)
                : null;
            var opsCase = new OpsCase
            {
                Id = StableGuid($"case:{index}"),
                CaseNumber = $"OPF-{baselineUtc.Year}-{index:0000}",
                Title = CreateTitle(caseType.Name, index),
                Description = CreateDescription(caseType.Name, priority, index),
                CaseTypeId = caseType.Id,
                Priority = priority,
                Status = status,
                AssignedToUserId = assignedTo?.Id,
                CreatedByUserId = index % 5 == 0 ? admin.Id : manager.Id,
                DueAtUtc = dueAtUtc,
                ResolvedAtUtc = resolvedAtUtc,
                ClosedAtUtc = closedAtUtc,
                CreatedAtUtc = createdAtUtc,
                UpdatedAtUtc = closedAtUtc ?? resolvedAtUtc ?? createdAtUtc.AddHours(index % 16)
            };
            cases.Add(opsCase);

            statusHistories.Add(new StatusHistory
            {
                Id = StableGuid($"status-history:{index}:created"),
                CaseId = opsCase.Id,
                FromStatus = null,
                ToStatus = CaseStatus.New,
                ChangedByUserId = opsCase.CreatedByUserId,
                Reason = "Case opened from operations intake.",
                CreatedAtUtc = createdAtUtc
            });

            if (assignedTo is not null)
            {
                statusHistories.Add(new StatusHistory
                {
                    Id = StableGuid($"status-history:{index}:assigned"),
                    CaseId = opsCase.Id,
                    FromStatus = CaseStatus.New,
                    ToStatus = CaseStatus.Assigned,
                    ChangedByUserId = manager.Id,
                    Reason = "Assigned for analyst review.",
                    CreatedAtUtc = createdAtUtc.AddMinutes(30)
                });

                assignmentHistories.Add(new AssignmentHistory
                {
                    Id = StableGuid($"assignment:{index}:initial"),
                    CaseId = opsCase.Id,
                    FromUserId = null,
                    ToUserId = assignedTo.Id,
                    AssignedByUserId = manager.Id,
                    Reason = "Initial assignment for operations review.",
                    CreatedAtUtc = createdAtUtc.AddMinutes(25)
                });
            }

            if (index % 2 == 0)
            {
                notes.Add(new CaseNote
                {
                    Id = StableGuid($"note:{index}:initial"),
                    CaseId = opsCase.Id,
                    AuthorUserId = assignedTo?.Id ?? manager.Id,
                    Body = "Initial review completed; supporting details captured for follow-up.",
                    CreatedAtUtc = createdAtUtc.AddHours(2)
                });
            }

            if (index % 5 == 0)
            {
                notes.Add(new CaseNote
                {
                    Id = StableGuid($"note:{index}:follow-up"),
                    CaseId = opsCase.Id,
                    AuthorUserId = manager.Id,
                    Body = "Manager follow-up requested to keep the exception moving.",
                    CreatedAtUtc = createdAtUtc.AddHours(6)
                });
            }

            if (index is >= 46 and <= 60)
            {
                var isPending = index <= 55;
                if (isPending)
                {
                    opsCase.Status = CaseStatus.PendingApproval;
                    opsCase.ClosedAtUtc = null;
                    opsCase.UpdatedAtUtc = createdAtUtc.AddHours(14);
                }

                approvalRequests.Add(new ApprovalRequest
                {
                    Id = StableGuid($"approval:{index}"),
                    CaseId = opsCase.Id,
                    RequestedByUserId = assignedTo!.Id,
                    ReviewedByUserId = isPending ? null : manager.Id,
                    Status = isPending ? ApprovalStatus.Pending : index % 2 == 0 ? ApprovalStatus.Approved : ApprovalStatus.Rejected,
                    RequestReason = "High-priority case appears resolved and needs manager closure review.",
                    DecisionReason = isPending ? null : "Seeded decision sample for approval history.",
                    RequestedAtUtc = createdAtUtc.AddHours(14),
                    DecisionAtUtc = isPending ? null : createdAtUtc.AddHours(24)
                });

                auditLogs.Add(new AuditLog
                {
                    Id = StableGuid($"audit:{index}:approval-requested"),
                    ActorUserId = assignedTo.Id,
                    EntityType = "Case",
                    EntityId = opsCase.Id,
                    Action = AuditAction.ClosureRequested,
                    MetadataJson = JsonSerializer.Serialize(new { opsCase.CaseNumber }, JsonOptions),
                    CorrelationId = $"seed-{index:0000}",
                    CreatedAtUtc = createdAtUtc.AddHours(14)
                });
            }

            auditLogs.Add(new AuditLog
            {
                Id = StableGuid($"audit:{index}:created"),
                ActorUserId = opsCase.CreatedByUserId,
                EntityType = "Case",
                EntityId = opsCase.Id,
                Action = AuditAction.CaseCreated,
                NewValuesJson = JsonSerializer.Serialize(new
                {
                    opsCase.CaseNumber,
                    Priority = opsCase.Priority.ToString(),
                    Status = opsCase.Status.ToString()
                }, JsonOptions),
                CorrelationId = $"seed-{index:0000}",
                CreatedAtUtc = createdAtUtc
            });
        }

        return new SeedDataSet(
            roles,
            users,
            userRoles,
            caseTypes,
            slaRules,
            cases,
            notes,
            statusHistories,
            assignmentHistories,
            approvalRequests,
            auditLogs);
    }

    private static List<AppUser> CreateUsers(DateTime nowUtc)
    {
        var passwordHasher = new PasswordHasher<AppUser>();

        return UserSeeds.Select(seed => new AppUser
        {
            Id = StableGuid($"user:{seed.Email}"),
            Email = seed.Email,
            NormalizedEmail = Normalize(seed.Email),
            UserName = seed.Email,
            NormalizedUserName = Normalize(seed.Email),
            EmailConfirmed = true,
            DisplayName = seed.DisplayName,
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        })
        .Select(user =>
        {
            user.PasswordHash = passwordHasher.HashPassword(user, DemoPassword);
            return user;
        })
        .ToList();
    }

    private static List<IdentityRole<Guid>> CreateRoles()
    {
        return OpsFlowRoles.All.Select(roleName => new IdentityRole<Guid>
        {
            Id = StableGuid($"role:{roleName}"),
            Name = roleName,
            NormalizedName = Normalize(roleName),
            ConcurrencyStamp = StableGuid($"role-stamp:{roleName}").ToString()
        }).ToList();
    }

    private static List<IdentityUserRole<Guid>> CreateUserRoles(
        IReadOnlyCollection<AppUser> users,
        IReadOnlyCollection<IdentityRole<Guid>> roles)
    {
        var rolesByName = roles.ToDictionary(x => x.Name!);
        return UserSeeds.Select(seed => new IdentityUserRole<Guid>
        {
            UserId = users.Single(x => x.Email == seed.Email).Id,
            RoleId = rolesByName[seed.Role].Id
        }).ToList();
    }

    private static List<CaseType> CreateCaseTypes(DateTime nowUtc)
    {
        return CaseTypeSeeds.Select(seed => new CaseType
        {
            Id = StableGuid($"case-type:{seed.Name}"),
            Name = seed.Name,
            Description = seed.Description,
            IsActive = true,
            CreatedAtUtc = nowUtc,
            UpdatedAtUtc = nowUtc
        }).ToList();
    }

    private static List<SlaRule> CreateSlaRules(IReadOnlyCollection<CaseType> caseTypes, DateTime nowUtc)
    {
        return caseTypes
            .SelectMany(caseType => Priorities.Select(priority => new SlaRule
            {
                Id = StableGuid($"sla:{caseType.Name}:{priority}"),
                CaseTypeId = caseType.Id,
                Priority = priority,
                TargetHours = TargetHours(priority),
                IsActive = true,
                CreatedAtUtc = nowUtc,
                UpdatedAtUtc = nowUtc
            }))
            .ToList();
    }

    private static int TargetHours(CasePriority priority)
    {
        return priority switch
        {
            CasePriority.Critical => 8,
            CasePriority.High => 24,
            CasePriority.Medium => 72,
            CasePriority.Low => 120,
            _ => throw new ArgumentOutOfRangeException(nameof(priority), priority, null)
        };
    }

    private static string CreateTitle(string caseTypeName, int index)
    {
        var subject = (index % 4) switch
        {
            0 => "regional operations queue",
            1 => "vendor record",
            2 => "customer account",
            _ => "fulfillment review"
        };

        return $"{caseTypeName} for {subject} #{index:0000}";
    }

    private static string CreateDescription(string caseTypeName, CasePriority priority, int index)
    {
        return $"{caseTypeName} exception generated for deterministic demo data. Priority is {priority}; reference batch {index % 12:00}.";
    }

    private static Guid StableGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes($"opsflow:{value}"));
        return new Guid(bytes);
    }

    private static string Normalize(string value) => value.ToUpperInvariant();
}
