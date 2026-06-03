using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Api.Tests.Auth;
using OpsFlow.Application.Auth;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Api.Tests.Dashboard;

public sealed class DashboardEndpointTests(OpsFlowApiFactory factory) : IClassFixture<OpsFlowApiFactory>
{
    [Fact]
    public async Task Get_summary_without_token_returns_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_breakdowns_without_token_returns_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/dashboard/breakdowns");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("analyst1@opsflow.local", "/api/dashboard/summary")]
    [InlineData("analyst1@opsflow.local", "/api/dashboard/breakdowns")]
    [InlineData("manager@opsflow.local", "/api/dashboard/summary")]
    [InlineData("manager@opsflow.local", "/api/dashboard/breakdowns")]
    [InlineData("admin@opsflow.local", "/api/dashboard/summary")]
    [InlineData("admin@opsflow.local", "/api/dashboard/breakdowns")]
    public async Task Authorized_roles_can_get_dashboard_endpoints(string email, string path)
    {
        var client = await CreateAuthenticatedClientAsync(email);

        var response = await client.GetAsync(path);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Summary_metrics_are_sql_backed_and_role_scoped()
    {
        var data = await ResetAndCreateDashboardDataAsync();
        var analystClient = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");
        var managerClient = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var analystResponse = await analystClient.GetAsync("/api/dashboard/summary");
        var managerResponse = await managerClient.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, analystResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
        var analystSummary = await ReadSummaryAsync(analystResponse);
        var managerSummary = await ReadSummaryAsync(managerResponse);

        Assert.Equal(2, analystSummary.OpenCases);
        Assert.Equal(1, analystSummary.OverdueOpenCases);
        Assert.Equal(1, analystSummary.PendingApprovals);
        Assert.Equal(4, analystSummary.AverageOpenAgeHours, precision: 5);
        Assert.Equal(0.5, analystSummary.SlaBreachRate, precision: 5);

        Assert.Equal(3, managerSummary.OpenCases);
        Assert.Equal(2, managerSummary.OverdueOpenCases);
        Assert.Equal(2, managerSummary.PendingApprovals);
        Assert.Equal(5, managerSummary.AverageOpenAgeHours, precision: 5);
        Assert.Equal(2 / 3d, managerSummary.SlaBreachRate, precision: 5);

        Assert.Contains(data.Analyst1OpenOverdueCaseId, data.AllCreatedCaseIds);
    }

    [Fact]
    public async Task Summary_returns_zero_average_and_breach_rate_when_no_open_cases()
    {
        await ResetDashboardDataAsync();
        var managerClient = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await managerClient.GetAsync("/api/dashboard/summary");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var summary = await ReadSummaryAsync(response);
        Assert.Equal(0, summary.OpenCases);
        Assert.Equal(0, summary.OverdueOpenCases);
        Assert.Equal(0, summary.PendingApprovals);
        Assert.Equal(0, summary.AverageOpenAgeHours);
        Assert.Equal(0, summary.SlaBreachRate);
    }

    [Fact]
    public async Task Breakdowns_are_grouped_and_role_scoped()
    {
        var data = await ResetAndCreateDashboardDataAsync();
        var analystClient = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");
        var managerClient = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var analystResponse = await analystClient.GetAsync("/api/dashboard/breakdowns");
        var managerResponse = await managerClient.GetAsync("/api/dashboard/breakdowns");

        Assert.Equal(HttpStatusCode.OK, analystResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, managerResponse.StatusCode);
        var analystBreakdowns = await ReadBreakdownsAsync(analystResponse);
        var managerBreakdowns = await ReadBreakdownsAsync(managerResponse);

        AssertBreakdownCount(analystBreakdowns.ByStatus, nameof(CaseStatus.Assigned), 1);
        AssertBreakdownCount(analystBreakdowns.ByStatus, nameof(CaseStatus.PendingApproval), 1);
        Assert.DoesNotContain(analystBreakdowns.ByStatus, x => x.Key == nameof(CaseStatus.InReview));
        AssertBreakdownCount(analystBreakdowns.ByPriority, nameof(CasePriority.High), 1);
        AssertBreakdownCount(analystBreakdowns.ByPriority, nameof(CasePriority.Critical), 1);
        AssertBreakdownCount(analystBreakdowns.ByCaseType, data.CaseTypeId.ToString(), 3);
        AssertBreakdownCount(analystBreakdowns.ByAssignee, data.Analyst1Id.ToString(), 3);
        Assert.DoesNotContain(analystBreakdowns.ByAssignee, x => x.Key == data.Analyst2Id.ToString());

        AssertBreakdownCount(managerBreakdowns.ByStatus, nameof(CaseStatus.Assigned), 1);
        AssertBreakdownCount(managerBreakdowns.ByStatus, nameof(CaseStatus.PendingApproval), 1);
        AssertBreakdownCount(managerBreakdowns.ByStatus, nameof(CaseStatus.InReview), 1);
        AssertBreakdownCount(managerBreakdowns.ByStatus, nameof(CaseStatus.Closed), 1);
        AssertBreakdownCount(managerBreakdowns.ByPriority, nameof(CasePriority.Medium), 1);
        AssertBreakdownCount(managerBreakdowns.ByAssignee, data.Analyst1Id.ToString(), 3);
        AssertBreakdownCount(managerBreakdowns.ByAssignee, data.Analyst2Id.ToString(), 1);

        var statusItem = managerBreakdowns.ByStatus.Single(x => x.Key == nameof(CaseStatus.Assigned));
        Assert.Equal(nameof(CaseStatus.Assigned), statusItem.RouteQuery?["status"]);
        var assigneeItem = managerBreakdowns.ByAssignee.Single(x => x.Key == data.Analyst1Id.ToString());
        Assert.Equal(data.Analyst1Id.ToString(), assigneeItem.RouteQuery?["assignedToUserId"]);
    }

    [Fact]
    public async Task Dashboard_overdue_count_matches_case_queue_overdue_count_for_same_role()
    {
        await ResetAndCreateDashboardDataAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var dashboardResponse = await client.GetAsync("/api/dashboard/summary");
        var casesResponse = await client.GetAsync("/api/cases?overdue=true&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, casesResponse.StatusCode);
        var summary = await ReadSummaryAsync(dashboardResponse);
        var cases = await casesResponse.Content.ReadFromJsonAsync<PagedCaseResponseBody>();
        Assert.NotNull(cases);
        Assert.Equal(summary.OverdueOpenCases, cases.TotalCount);
    }

    [Fact]
    public async Task Manager_pending_approvals_count_matches_pending_approval_queue()
    {
        await ResetAndCreateDashboardDataAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var dashboardResponse = await client.GetAsync("/api/dashboard/summary");
        var approvalsResponse = await client.GetAsync("/api/approvals/pending?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, dashboardResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, approvalsResponse.StatusCode);
        var summary = await ReadSummaryAsync(dashboardResponse);
        var approvals = await approvalsResponse.Content.ReadFromJsonAsync<PagedApprovalResponseBody>();
        Assert.NotNull(approvals);
        Assert.Equal(summary.PendingApprovals, approvals.TotalCount);
    }

    private async Task<DashboardSeedData> ResetAndCreateDashboardDataAsync()
    {
        await ResetDashboardDataAsync();
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        var analyst1Id = await GetUserIdAsync(dbContext, "analyst1@opsflow.local");
        var analyst2Id = await GetUserIdAsync(dbContext, "analyst2@opsflow.local");
        var managerId = await GetUserIdAsync(dbContext, "manager@opsflow.local");
        var caseTypeId = await dbContext.CaseTypes.Select(x => x.Id).FirstAsync();
        var nowUtc = OpsFlowApiFactory.FixedNowUtc;

        var analyst1OpenOverdue = CreateCase(
            "Dashboard analyst overdue",
            caseTypeId,
            managerId,
            CaseStatus.Assigned,
            CasePriority.High,
            nowUtc.AddHours(-6),
            nowUtc.AddHours(-1),
            analyst1Id);
        var analyst1PendingNotOverdue = CreateCase(
            "Dashboard analyst pending",
            caseTypeId,
            managerId,
            CaseStatus.PendingApproval,
            CasePriority.Critical,
            nowUtc.AddHours(-2),
            nowUtc.AddHours(2),
            analyst1Id);
        var analyst2OpenOverdue = CreateCase(
            "Dashboard other analyst overdue",
            caseTypeId,
            managerId,
            CaseStatus.InReview,
            CasePriority.Medium,
            nowUtc.AddHours(-7),
            nowUtc.AddHours(-2),
            analyst2Id);
        var closedOverdue = CreateCase(
            "Dashboard closed overdue",
            caseTypeId,
            managerId,
            CaseStatus.Closed,
            CasePriority.Low,
            nowUtc.AddHours(-20),
            nowUtc.AddHours(-10),
            analyst1Id);

        dbContext.Cases.AddRange(analyst1OpenOverdue, analyst1PendingNotOverdue, analyst2OpenOverdue, closedOverdue);
        dbContext.ApprovalRequests.AddRange(
            CreateApproval(analyst1PendingNotOverdue.Id, analyst1Id, ApprovalStatus.Pending, nowUtc.AddHours(-1)),
            CreateApproval(analyst2OpenOverdue.Id, analyst2Id, ApprovalStatus.Pending, nowUtc.AddHours(-1)),
            CreateApproval(analyst1OpenOverdue.Id, analyst1Id, ApprovalStatus.Approved, nowUtc.AddHours(-3), managerId),
            CreateApproval(closedOverdue.Id, analyst1Id, ApprovalStatus.Rejected, nowUtc.AddHours(-4), managerId));
        await dbContext.SaveChangesAsync();

        return new DashboardSeedData(
            analyst1Id,
            analyst2Id,
            caseTypeId,
            analyst1OpenOverdue.Id,
            [analyst1OpenOverdue.Id, analyst1PendingNotOverdue.Id, analyst2OpenOverdue.Id, closedOverdue.Id]);
    }

    private async Task ResetDashboardDataAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        dbContext.ApprovalRequests.RemoveRange(dbContext.ApprovalRequests);
        dbContext.StatusHistories.RemoveRange(dbContext.StatusHistories);
        dbContext.AssignmentHistories.RemoveRange(dbContext.AssignmentHistories);
        dbContext.CaseNotes.RemoveRange(dbContext.CaseNotes);
        dbContext.AuditLogs.RemoveRange(dbContext.AuditLogs);
        dbContext.Cases.RemoveRange(dbContext.Cases);
        await dbContext.SaveChangesAsync();
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string email)
    {
        var client = factory.CreateClient();
        var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, "Password123!"));
        loginResponse.EnsureSuccessStatusCode();
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseBody>();
        Assert.NotNull(login);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        return client;
    }

    private static async Task<DashboardSummaryBody> ReadSummaryAsync(HttpResponseMessage response)
    {
        var summary = await response.Content.ReadFromJsonAsync<DashboardSummaryBody>();
        Assert.NotNull(summary);
        return summary;
    }

    private static async Task<DashboardBreakdownsBody> ReadBreakdownsAsync(HttpResponseMessage response)
    {
        var breakdowns = await response.Content.ReadFromJsonAsync<DashboardBreakdownsBody>();
        Assert.NotNull(breakdowns);
        return breakdowns;
    }

    private static void AssertBreakdownCount(
        IReadOnlyList<DashboardBreakdownItemBody> breakdown,
        string key,
        int expectedCount)
    {
        var item = breakdown.Single(x => x.Key == key);
        Assert.Equal(expectedCount, item.Count);
    }

    private static Task<Guid> GetUserIdAsync(OpsFlowDbContext dbContext, string email)
    {
        return dbContext.Users
            .Where(x => x.Email == email)
            .Select(x => x.Id)
            .SingleAsync();
    }

    private static OpsCase CreateCase(
        string title,
        Guid caseTypeId,
        Guid createdByUserId,
        CaseStatus status,
        CasePriority priority,
        DateTime createdAtUtc,
        DateTime dueAtUtc,
        Guid? assignedToUserId)
    {
        return new OpsCase
        {
            Id = Guid.NewGuid(),
            CaseNumber = $"OPF-2026-{Random.Shared.Next(5000, 9999):0000}-{Guid.NewGuid():N}"[..18],
            Title = title,
            Description = "Synthetic internal operations case for dashboard tests.",
            CaseTypeId = caseTypeId,
            Priority = priority,
            Status = status,
            AssignedToUserId = assignedToUserId,
            CreatedByUserId = createdByUserId,
            DueAtUtc = dueAtUtc,
            ClosedAtUtc = status == CaseStatus.Closed ? OpsFlowApiFactory.FixedNowUtc : null,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc,
            RowVersion = Guid.NewGuid().ToByteArray()
        };
    }

    private static ApprovalRequest CreateApproval(
        Guid caseId,
        Guid requestedByUserId,
        ApprovalStatus status,
        DateTime requestedAtUtc,
        Guid? reviewedByUserId = null)
    {
        return new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            RequestedByUserId = requestedByUserId,
            ReviewedByUserId = reviewedByUserId,
            Status = status,
            RequestReason = "Work is complete and ready for manager closure review.",
            DecisionReason = status == ApprovalStatus.Pending ? null : "Decision recorded.",
            RequestedAtUtc = requestedAtUtc,
            DecisionAtUtc = status == ApprovalStatus.Pending ? null : OpsFlowApiFactory.FixedNowUtc
        };
    }

    private sealed record DashboardSeedData(
        Guid Analyst1Id,
        Guid Analyst2Id,
        Guid CaseTypeId,
        Guid Analyst1OpenOverdueCaseId,
        IReadOnlyList<Guid> AllCreatedCaseIds);

    private sealed record LoginResponseBody(
        [property: JsonPropertyName("accessToken")] string AccessToken);

    private sealed record DashboardSummaryBody(
        [property: JsonPropertyName("openCases")] int OpenCases,
        [property: JsonPropertyName("overdueOpenCases")] int OverdueOpenCases,
        [property: JsonPropertyName("pendingApprovals")] int PendingApprovals,
        [property: JsonPropertyName("averageOpenAgeHours")] double AverageOpenAgeHours,
        [property: JsonPropertyName("slaBreachRate")] double SlaBreachRate);

    private sealed record DashboardBreakdownsBody(
        [property: JsonPropertyName("byStatus")] IReadOnlyList<DashboardBreakdownItemBody> ByStatus,
        [property: JsonPropertyName("byPriority")] IReadOnlyList<DashboardBreakdownItemBody> ByPriority,
        [property: JsonPropertyName("byCaseType")] IReadOnlyList<DashboardBreakdownItemBody> ByCaseType,
        [property: JsonPropertyName("byAssignee")] IReadOnlyList<DashboardBreakdownItemBody> ByAssignee);

    private sealed record DashboardBreakdownItemBody(
        [property: JsonPropertyName("key")] string Key,
        [property: JsonPropertyName("label")] string Label,
        [property: JsonPropertyName("count")] int Count,
        [property: JsonPropertyName("routeQuery")] IReadOnlyDictionary<string, string>? RouteQuery);

    private sealed record PagedCaseResponseBody(
        [property: JsonPropertyName("totalCount")] int TotalCount);

    private sealed record PagedApprovalResponseBody(
        [property: JsonPropertyName("totalCount")] int TotalCount);
}
