using OpsFlow.Api.Data.Enums;
using OpsFlow.Api.Data.Seed;

namespace OpsFlow.Api.Tests.Data;

public sealed class SeedDataGeneratorTests
{
    private static readonly DateTime FixedNowUtc = new(2026, 5, 15, 12, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Generate_creates_at_least_300_cases()
    {
        var seedData = SeedDataGenerator.Generate(FixedNowUtc);

        Assert.True(seedData.Cases.Count >= 300);
    }

    [Fact]
    public void Generate_includes_admin_manager_and_analyst_users()
    {
        var roles = SeedDataGenerator.Generate(FixedNowUtc).Users.Select(x => x.Role).ToHashSet();

        Assert.Contains(UserRole.Admin, roles);
        Assert.Contains(UserRole.Manager, roles);
        Assert.Contains(UserRole.Analyst, roles);
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
        var pendingApprovals = SeedDataGenerator.Generate(FixedNowUtc).ApprovalRequests
            .Count(x => x.Status == ApprovalStatus.Pending);

        Assert.True(pendingApprovals >= 10);
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
