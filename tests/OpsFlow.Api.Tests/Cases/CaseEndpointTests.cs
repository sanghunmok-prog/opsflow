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
        var client = await CreateAuthenticatedClientAsync("manager@opsflow.local");

        var response = await client.GetAsync("/api/cases?page=2&pageSize=10");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var cases = await ReadPagedCasesAsync(response);
        Assert.Equal(2, cases.Page);
        Assert.Equal(10, cases.PageSize);
        Assert.Equal(320, cases.TotalCount);
        Assert.Equal(32, cases.TotalPages);
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

    private static async Task<PagedCaseResponseBody> ReadPagedCasesAsync(HttpResponseMessage response)
    {
        var cases = await response.Content.ReadFromJsonAsync<PagedCaseResponseBody>();
        Assert.NotNull(cases);
        return cases;
    }

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
        [property: JsonPropertyName("caseType")] SummaryBody CaseType,
        [property: JsonPropertyName("priority")] string Priority,
        [property: JsonPropertyName("status")] string Status,
        [property: JsonPropertyName("assignedTo")] SummaryBody? AssignedTo,
        [property: JsonPropertyName("createdAtUtc")] DateTime CreatedAtUtc,
        [property: JsonPropertyName("dueAtUtc")] DateTime DueAtUtc);

    private sealed record CaseDetailBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("assignedTo")] SummaryBody? AssignedTo);

    private sealed record SummaryBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("displayName")] string DisplayName);
}
