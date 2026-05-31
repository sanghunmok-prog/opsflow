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

namespace OpsFlow.Api.Tests.Cases;

public sealed class CaseEndpointTests(OpsFlowApiFactory factory) : IClassFixture<OpsFlowApiFactory>
{
    [Fact]
    public async Task Get_cases_without_token_returns_unauthorized()
    {
        var response = await factory.CreateClient().GetAsync("/api/cases");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Get_case_detail_without_token_returns_unauthorized()
    {
        var caseId = await GetAnyCaseIdAsync();

        var response = await factory.CreateClient().GetAsync($"/api/cases/{caseId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Create_case_without_token_returns_unauthorized()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();

        var response = await factory.CreateClient().PostAsJsonAsync("/api/cases", CreateCaseBody(caseTypeId));

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Analyst_cannot_create_case()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var client = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");

        var response = await client.PostAsJsonAsync("/api/cases", CreateCaseBody(caseTypeId));

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_can_create_case()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.PostAsJsonAsync("/api/cases", CreateCaseBody(caseTypeId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_create_case()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var client = await CreateAuthenticatedClientAsync("admin@opsflow.local");

        var response = await client.PostAsJsonAsync("/api/cases", CreateCaseBody(caseTypeId));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task Analyst_list_returns_only_assigned_cases()
    {
        var analystId = await GetUserIdAsync("analyst1@opsflow.local");
        var client = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");

        var response = await client.GetAsync("/api/cases?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.NotEmpty(cases.Items);
        Assert.All(cases.Items, item => Assert.Equal(analystId, item.AssignedTo?.Id));
    }

    [Fact]
    public async Task Analyst_list_for_another_assignee_returns_forbidden()
    {
        var otherAnalystId = await GetUserIdAsync("analyst2@opsflow.local");
        var client = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");

        var response = await client.GetAsync($"/api/cases?assignedToUserId={otherAnalystId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_list_can_return_cases_across_multiple_assignees()
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync("/api/cases?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.True(cases.Items.Select(x => x.AssignedTo?.Id).Where(x => x is not null).Distinct().Count() > 1);
    }

    [Fact]
    public async Task Admin_list_can_return_cases_across_multiple_assignees()
    {
        var client = await CreateAuthenticatedClientAsync("admin@opsflow.local");

        var response = await client.GetAsync("/api/cases?pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.True(cases.Items.Select(x => x.AssignedTo?.Id).Where(x => x is not null).Distinct().Count() > 1);
    }

    [Fact]
    public async Task Analyst_can_access_assigned_case_detail()
    {
        var analystId = await GetUserIdAsync("analyst1@opsflow.local");
        var caseId = await GetCaseAssignedToAsync(analystId);
        var client = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");

        var response = await client.GetAsync($"/api/cases/{caseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<CaseDetailBody>();
        Assert.NotNull(detail);
        Assert.Equal(caseId, detail.Id);
        Assert.Equal(analystId, detail.AssignedTo?.Id);
    }

    [Fact]
    public async Task Analyst_cannot_access_another_analysts_case_detail()
    {
        var analystId = await GetUserIdAsync("analyst1@opsflow.local");
        var caseId = await GetCaseNotAssignedToAsync(analystId);
        var client = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");

        var response = await client.GetAsync($"/api/cases/{caseId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Manager_can_access_any_case_detail()
    {
        var analystId = await GetUserIdAsync("analyst1@opsflow.local");
        var caseId = await GetCaseAssignedToAsync(analystId);
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync($"/api/cases/{caseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Admin_can_access_any_case_detail()
    {
        var analystId = await GetUserIdAsync("analyst1@opsflow.local");
        var caseId = await GetCaseAssignedToAsync(analystId);
        var client = await CreateAuthenticatedClientAsync("admin@opsflow.local");

        var response = await client.GetAsync($"/api/cases/{caseId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Missing_case_detail_returns_not_found()
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync($"/api/cases/{Guid.NewGuid()}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Pagination_returns_expected_metadata()
    {
        var expectedCount = await GetCaseCountAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync("/api/cases?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.Equal(2, cases.Page);
        Assert.Equal(10, cases.PageSize);
        Assert.Equal(expectedCount, cases.TotalCount);
        Assert.Equal((int)Math.Ceiling(expectedCount / 10d), cases.TotalPages);
        Assert.Equal(10, cases.Items.Count);
    }

    [Theory]
    [InlineData("/api/cases?page=0")]
    [InlineData("/api/cases?pageSize=0")]
    [InlineData("/api/cases?pageSize=101")]
    [InlineData("/api/cases?status=NotAStatus")]
    [InlineData("/api/cases?priority=NotAPriority")]
    [InlineData("/api/cases?sortBy=updatedAtUtc")]
    [InlineData("/api/cases?sortDirection=sideways")]
    public async Task Invalid_query_parameters_return_bad_request(string url)
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync(url);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Status_filter_works()
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync($"/api/cases?status={CaseStatus.InReview}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.NotEmpty(cases.Items);
        Assert.All(cases.Items, item => Assert.Equal(nameof(CaseStatus.InReview), item.Status));
    }

    [Fact]
    public async Task Priority_filter_works()
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync($"/api/cases?priority={CasePriority.High}&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.NotEmpty(cases.Items);
        Assert.All(cases.Items, item => Assert.Equal(nameof(CasePriority.High), item.Priority));
    }

    [Fact]
    public async Task Search_by_case_number_and_title_works()
    {
        var seedCase = await GetAnyCaseAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var byCaseNumber = await client.GetAsync($"/api/cases?search={Uri.EscapeDataString(seedCase.CaseNumber)}");
        var byTitle = await client.GetAsync($"/api/cases?search={Uri.EscapeDataString(seedCase.Title)}");

        Assert.Equal(HttpStatusCode.OK, byCaseNumber.StatusCode);
        Assert.Equal(HttpStatusCode.OK, byTitle.StatusCode);
        Assert.Contains((await ReadPagedCasesAsync(byCaseNumber)).Items, item => item.Id == seedCase.Id);
        Assert.Contains((await ReadPagedCasesAsync(byTitle)).Items, item => item.Id == seedCase.Id);
    }

    [Fact]
    public async Task Create_case_sets_defaults_sla_due_date_response_and_audit()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var creatorId = await GetUserIdAsync("manager@opsflow.local");
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.PostAsJsonAsync(
            "/api/cases",
            CreateCaseBody(caseTypeId, "Critical"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CaseDetailBody>();
        Assert.NotNull(created);
        Assert.StartsWith("OPF-2026-", created.CaseNumber, StringComparison.Ordinal);
        Assert.Equal("New", created.Status);
        Assert.Null(created.AssignedTo);
        Assert.Equal(creatorId, created.CreatedBy.Id);
        Assert.Equal(OpsFlowApiFactory.FixedNowUtc, created.CreatedAtUtc);
        Assert.Equal(OpsFlowApiFactory.FixedNowUtc, created.UpdatedAtUtc);
        Assert.Equal(OpsFlowApiFactory.FixedNowUtc.AddHours(8), created.DueAtUtc);
        Assert.False(created.IsOverdue);

        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        var auditLog = await dbContext.AuditLogs.SingleAsync(x =>
            x.EntityType == "Case" &&
            x.EntityId == created.Id &&
            x.Action == AuditAction.CaseCreated);
        Assert.Equal(creatorId, auditLog.ActorUserId);
        Assert.Equal(OpsFlowApiFactory.FixedNowUtc, auditLog.CreatedAtUtc);
    }

    [Fact]
    public async Task Create_high_case_due_date_uses_24_hour_sla()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.PostAsJsonAsync(
            "/api/cases",
            CreateCaseBody(caseTypeId, "High"));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<CaseDetailBody>();
        Assert.NotNull(created);
        Assert.Equal(OpsFlowApiFactory.FixedNowUtc.AddHours(24), created.DueAtUtc);
    }

    [Fact]
    public async Task Create_case_validation_errors_return_expected_statuses()
    {
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var missingTitle = await client.PostAsJsonAsync("/api/cases", new
        {
            description = "Synthetic internal operations case.",
            caseTypeId,
            priority = "High"
        });
        var invalidPriority = await client.PostAsJsonAsync("/api/cases", CreateCaseBody(caseTypeId, "Urgent"));
        var missingCaseType = await client.PostAsJsonAsync("/api/cases", new
        {
            title = "Vendor onboarding exception",
            description = "Synthetic internal operations case.",
            priority = "High"
        });
        var unknownCaseType = await client.PostAsJsonAsync("/api/cases", CreateCaseBody(Guid.NewGuid(), "High"));
        var noSlaCaseTypeId = await CreateCaseTypeWithoutSlaAsync();
        var missingSla = await client.PostAsJsonAsync("/api/cases", CreateCaseBody(noSlaCaseTypeId, "High"));

        Assert.Equal(HttpStatusCode.BadRequest, missingTitle.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, invalidPriority.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, missingCaseType.StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, unknownCaseType.StatusCode);
        Assert.Equal(HttpStatusCode.UnprocessableEntity, missingSla.StatusCode);
    }

    [Fact]
    public async Task Case_list_and_detail_include_query_time_overdue()
    {
        var createdById = await GetUserIdAsync("manager@opsflow.local");
        var caseTypeId = await GetAnyCaseTypeIdAsync();
        var overdueOpenId = await CreateDirectCaseAsync(
            "PR-04 overdue open",
            caseTypeId,
            createdById,
            CaseStatus.InReview,
            OpsFlowApiFactory.FixedNowUtc.AddHours(-1));
        var overdueClosedId = await CreateDirectCaseAsync(
            "PR-04 overdue closed",
            caseTypeId,
            createdById,
            CaseStatus.Closed,
            OpsFlowApiFactory.FixedNowUtc.AddHours(-1));
        var futureOpenId = await CreateDirectCaseAsync(
            "PR-04 future open",
            caseTypeId,
            createdById,
            CaseStatus.New,
            OpsFlowApiFactory.FixedNowUtc.AddHours(1));
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var listResponse = await client.GetAsync("/api/cases?pageSize=100&sortBy=caseNumber&sortDirection=desc");
        var overdueDetailResponse = await client.GetAsync($"/api/cases/{overdueOpenId}");
        var closedDetailResponse = await client.GetAsync($"/api/cases/{overdueClosedId}");
        var futureDetailResponse = await client.GetAsync($"/api/cases/{futureOpenId}");

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        var cases = await ReadPagedCasesAsync(listResponse);
        Assert.Contains(cases.Items, x => x.Id == overdueOpenId && x.IsOverdue);
        Assert.Contains(cases.Items, x => x.Id == overdueClosedId && !x.IsOverdue);
        Assert.Contains(cases.Items, x => x.Id == futureOpenId && !x.IsOverdue);
        Assert.True((await ReadDetailAsync(overdueDetailResponse)).IsOverdue);
        Assert.False((await ReadDetailAsync(closedDetailResponse)).IsOverdue);
        Assert.False((await ReadDetailAsync(futureDetailResponse)).IsOverdue);
    }

    [Fact]
    public async Task Overdue_filter_returns_matching_cases()
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var overdueResponse = await client.GetAsync("/api/cases?overdue=true&pageSize=100");
        var notOverdueResponse = await client.GetAsync("/api/cases?overdue=false&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, overdueResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, notOverdueResponse.StatusCode);
        var overdueCases = await ReadPagedCasesAsync(overdueResponse);
        var notOverdueCases = await ReadPagedCasesAsync(notOverdueResponse);
        Assert.NotEmpty(overdueCases.Items);
        Assert.NotEmpty(notOverdueCases.Items);
        Assert.All(overdueCases.Items, item => Assert.True(item.IsOverdue));
        Assert.All(notOverdueCases.Items, item => Assert.False(item.IsOverdue));
    }

    [Fact]
    public async Task Analyst_overdue_filter_still_returns_only_assigned_cases()
    {
        var analystId = await GetUserIdAsync("analyst1@opsflow.local");
        var client = await CreateAuthenticatedClientAsync("analyst1@opsflow.local");

        var response = await client.GetAsync("/api/cases?overdue=true&pageSize=100");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.NotEmpty(cases.Items);
        Assert.All(cases.Items, item =>
        {
            Assert.True(item.IsOverdue);
            Assert.Equal(analystId, item.AssignedTo?.Id);
        });
    }

    [Fact]
    public async Task Sort_direction_asc_and_desc_work()
    {
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var ascResponse = await client.GetAsync("/api/cases?pageSize=5&sortBy=caseNumber&sortDirection=asc");
        var descResponse = await client.GetAsync("/api/cases?pageSize=5&sortBy=caseNumber&sortDirection=desc");

        Assert.Equal(HttpStatusCode.OK, ascResponse.StatusCode);
        Assert.Equal(HttpStatusCode.OK, descResponse.StatusCode);
        var ascCaseNumbers = (await ReadPagedCasesAsync(ascResponse)).Items.Select(x => x.CaseNumber).ToArray();
        var descCaseNumbers = (await ReadPagedCasesAsync(descResponse)).Items.Select(x => x.CaseNumber).ToArray();
        Assert.Equal(ascCaseNumbers.Order(StringComparer.Ordinal).ToArray(), ascCaseNumbers);
        Assert.Equal(descCaseNumbers.OrderByDescending(x => x, StringComparer.Ordinal).ToArray(), descCaseNumbers);
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

    private async Task<Guid> GetUserIdAsync(string email)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.Users
            .Where(x => x.Email == email)
            .Select(x => x.Id)
            .SingleAsync();
    }

    private async Task<Guid> GetAnyCaseIdAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.Cases.Select(x => x.Id).FirstAsync();
    }

    private async Task<int> GetCaseCountAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.Cases.CountAsync();
    }

    private async Task<Guid> GetAnyCaseTypeIdAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.CaseTypes.Select(x => x.Id).FirstAsync();
    }

    private async Task<OpsCase> GetAnyCaseAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.Cases.AsNoTracking().FirstAsync();
    }

    private async Task<Guid> GetCaseAssignedToAsync(Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.Cases
            .Where(x => x.AssignedToUserId == userId)
            .Select(x => x.Id)
            .FirstAsync();
    }

    private async Task<Guid> GetCaseNotAssignedToAsync(Guid userId)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        return await dbContext.Cases
            .Where(x => x.AssignedToUserId != null && x.AssignedToUserId != userId)
            .Select(x => x.Id)
            .FirstAsync();
    }

    private async Task<Guid> CreateCaseTypeWithoutSlaAsync()
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        var caseType = new CaseType
        {
            Id = Guid.NewGuid(),
            Name = $"PR-04 No SLA {Guid.NewGuid():N}",
            Description = "Case type without an active SLA rule.",
            IsActive = true,
            CreatedAtUtc = OpsFlowApiFactory.FixedNowUtc,
            UpdatedAtUtc = OpsFlowApiFactory.FixedNowUtc
        };

        dbContext.CaseTypes.Add(caseType);
        await dbContext.SaveChangesAsync();
        return caseType.Id;
    }

    private async Task<Guid> CreateDirectCaseAsync(
        string title,
        Guid caseTypeId,
        Guid createdByUserId,
        CaseStatus status,
        DateTime dueAtUtc)
    {
        await using var scope = factory.Services.CreateAsyncScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<OpsFlowDbContext>();
        var id = Guid.NewGuid();
        dbContext.Cases.Add(new OpsCase
        {
            Id = id,
            CaseNumber = $"OPF-2026-{Random.Shared.Next(5000, 9999):0000}-{Guid.NewGuid():N}"[..18],
            Title = title,
            Description = "Synthetic internal operations case for PR-04 tests.",
            CaseTypeId = caseTypeId,
            Priority = CasePriority.High,
            Status = status,
            CreatedByUserId = createdByUserId,
            DueAtUtc = dueAtUtc,
            ClosedAtUtc = status == CaseStatus.Closed ? OpsFlowApiFactory.FixedNowUtc : null,
            CreatedAtUtc = OpsFlowApiFactory.FixedNowUtc.AddHours(-2),
            UpdatedAtUtc = OpsFlowApiFactory.FixedNowUtc.AddHours(-1)
        });
        await dbContext.SaveChangesAsync();
        return id;
    }

    private static async Task<PagedCaseResponseBody> ReadPagedCasesAsync(HttpResponseMessage response)
    {
        var cases = await response.Content.ReadFromJsonAsync<PagedCaseResponseBody>();
        Assert.NotNull(cases);
        return cases;
    }

    private static async Task<CaseDetailBody> ReadDetailAsync(HttpResponseMessage response)
    {
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<CaseDetailBody>();
        Assert.NotNull(detail);
        return detail;
    }

    private static object CreateCaseBody(Guid caseTypeId, string priority = "High") => new
    {
        title = "Vendor onboarding exception",
        description = "Synthetic internal operations case.",
        caseTypeId,
        priority
    };

    private sealed record LoginResponseBody(
        [property: JsonPropertyName("accessToken")] string AccessToken);

    private sealed record PagedCaseResponseBody(
        [property: JsonPropertyName("items")] IReadOnlyList<CaseListItemBody> Items,
        [property: JsonPropertyName("page")] int Page,
        [property: JsonPropertyName("pageSize")] int PageSize,
        [property: JsonPropertyName("totalCount")] int TotalCount,
        [property: JsonPropertyName("totalPages")] int TotalPages);

    private sealed record CaseListItemBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("caseNumber")] string CaseNumber,
        [property: JsonPropertyName("title")] string Title,
        [property: JsonPropertyName("caseType")] CaseTypeSummaryBody CaseType,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("assignedTo")] SummaryBody? AssignedTo,
        [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
        [property: JsonPropertyName("dueAtUtc")] DateTime DueAtUtc,
        [property: JsonPropertyName("isOverdue")] bool IsOverdue);

    private sealed record CaseDetailBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("caseNumber")] string CaseNumber,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("assignedTo")] SummaryBody? AssignedTo,
        [property: JsonPropertyName("createdBy")] SummaryBody CreatedBy,
        [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
        [property: JsonPropertyName("updatedAtUtc")] DateTime UpdatedAtUtc,
        [property: JsonPropertyName("dueAtUtc")] DateTime DueAtUtc,
        [property: JsonPropertyName("isOverdue")] bool IsOverdue);

    private sealed record CaseTypeSummaryBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("name")] string Name);

    private sealed record SummaryBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("displayName")] string DisplayName);
}
