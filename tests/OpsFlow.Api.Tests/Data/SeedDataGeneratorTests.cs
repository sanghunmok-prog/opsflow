using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data.Seed;

namespace OpsFlow.Api.Tests.Data;

public sealed class SeedDataGeneratorTests
{
    private static readonly DateTime FixedNowUtc = new(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Generate_creates_at_least_300_cases()
    {
        var seedData = SeedDataGenerator.Generate(FixedNowUtc);

        Assert.InRange(seedData.Cases.Count, 300, 500);
    }

    [Fact]
    public void Generate_seeds_only_required_roles()
    {
        var roles = SeedDataGenerator.Generate(FixedNowUtc).Roles
            .Select(x => x.Name)
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(OpsFlowRoles.All.Order(StringComparer.Ordinal), roles);
    }

    [Fact]
    public void Generate_assigns_demo_users_to_required_roles()
    {
        var seedData = SeedDataGenerator.Generate(FixedNowUtc);

        Assert.Equal(5, seedData.Users.Count);
        Assert.Equal(seedData.Users.Count, seedData.UserRoles.Count);
    }

    [Fact]
    public void Generate_includes_all_required_case_types()
    {
        var caseTypeNames = SeedDataGenerator.Generate(FixedNowUtc).CaseTypes.Select(x => x.Name).ToHashSet();

        var expected =
            new[]
            {
                "Invoice Mismatch",
                "Delayed Shipment",
                "Missing Document",
                "Customer Escalation",
                "Duplicate Account",
                "Vendor Approval Issue"
            }
            .Order(StringComparer.Ordinal)
            .ToArray();

        Assert.Equal(expected, caseTypeNames.Order(StringComparer.Ordinal).ToArray());
    }

    [Fact]
    public void Generate_includes_sla_rules_for_each_case_type_and_priority()
    {
        var seedData = SeedDataGenerator.Generate(FixedNowUtc);
        var expectedCombinations = seedData.CaseTypes.Count * Enum.GetValues<CasePriority>().Length;
        var actualCombinations = seedData.SlaRules
            .Select(x => new { x.CaseTypeId, x.Priority })
            .Distinct()
            .Count();

        Assert.Equal(expectedCombinations, seedData.SlaRules.Count);
        Assert.Equal(expectedCombinations, actualCombinations);
    }

    [Fact]
    public void Generate_uses_locked_sla_target_hours()
    {
        var targetHoursByPriority = SeedDataGenerator.Generate(FixedNowUtc).SlaRules
            .GroupBy(x => x.Priority)
            .ToDictionary(x => x.Key, x => x.Select(rule => rule.TargetHours).Distinct().Single());

        Assert.Equal(120, targetHoursByPriority[CasePriority.Low]);
        Assert.Equal(72, targetHoursByPriority[CasePriority.Medium]);
        Assert.Equal(24, targetHoursByPriority[CasePriority.High]);
        Assert.Equal(8, targetHoursByPriority[CasePriority.Critical]);
    }

    [Fact]
    public void Generate_creates_unique_case_numbers()
    {
        var caseNumbers = SeedDataGenerator.Generate(FixedNowUtc).Cases.Select(x => x.CaseNumber).ToArray();

        Assert.Equal(caseNumbers.Length, caseNumbers.Distinct(StringComparer.Ordinal).Count());
    }

    [Fact]
    public void Generate_cases_reference_valid_users_and_case_types()
    {
        var seedData = SeedDataGenerator.Generate(FixedNowUtc);
        var userIds = seedData.Users.Select(x => x.Id).ToHashSet();
        var caseTypeIds = seedData.CaseTypes.Select(x => x.Id).ToHashSet();

        Assert.All(seedData.Cases, opsCase =>
        {
            Assert.Contains(opsCase.CreatedByUserId, userIds);
            Assert.Contains(opsCase.CaseTypeId, caseTypeIds);
            if (opsCase.AssignedToUserId.HasValue)
            {
                Assert.Contains(opsCase.AssignedToUserId.Value, userIds);
            }
        });
    }

    [Fact]
    public void Generate_includes_overdue_ready_cases_for_future_dashboard_demo()
    {
        var overdueReadyCases = SeedDataGenerator.Generate(FixedNowUtc).Cases
            .Where(x => x.Status is not CaseStatus.Closed and not CaseStatus.Resolved)
            .Count(x => x.DueAtUtc < FixedNowUtc);

        Assert.True(overdueReadyCases >= 30);
    }

    [Fact]
    public void Generate_includes_pending_approval_samples()
    {
        var seedData = SeedDataGenerator.Generate(FixedNowUtc);
        var casesById = seedData.Cases.ToDictionary(x => x.Id);
        var pendingApprovals = seedData.ApprovalRequests
            .Where(x => x.Status == ApprovalStatus.Pending)
            .ToArray();

        Assert.True(pendingApprovals.Length >= 10);
        Assert.All(pendingApprovals, approval =>
        {
            Assert.Equal(CaseStatus.PendingApproval, casesById[approval.CaseId].Status);
        });
    }

    [Fact]
    public void Generate_has_at_most_one_pending_approval_per_case()
    {
        var duplicatePendingApprovals = SeedDataGenerator.Generate(FixedNowUtc).ApprovalRequests
            .Where(x => x.Status == ApprovalStatus.Pending)
            .GroupBy(x => x.CaseId)
            .Where(x => x.Count() > 1);

        Assert.Empty(duplicatePendingApprovals);
    }

    [Fact]
    public void Generate_assignment_history_includes_required_reason()
    {
        var assignments = SeedDataGenerator.Generate(FixedNowUtc).AssignmentHistories;

        Assert.NotEmpty(assignments);
        Assert.All(assignments, assignment => Assert.False(string.IsNullOrWhiteSpace(assignment.Reason)));
    }

    [Fact]
    public void CaseStatus_includes_pending_approval()
    {
        Assert.Contains(CaseStatus.PendingApproval, Enum.GetValues<CaseStatus>());
    }

    [Fact]
    public void Generate_is_deterministic_for_same_clock()
    {
        var first = SeedDataGenerator.Generate(FixedNowUtc);
        var second = SeedDataGenerator.Generate(FixedNowUtc);

        Assert.Equal(first.Cases.Select(x => x.CaseNumber), second.Cases.Select(x => x.CaseNumber));
        Assert.Equal(first.Cases.Select(x => x.Id), second.Cases.Select(x => x.Id));
        Assert.Equal(first.ApprovalRequests.Select(x => x.Id), second.ApprovalRequests.Select(x => x.Id));
    }
}
