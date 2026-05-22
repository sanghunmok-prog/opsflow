using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using OpsFlow.Application.Auth;

namespace OpsFlow.Api.Tests.Auth;

public sealed class AuthEndpointTests(OpsFlowApiFactory factory) : IClassFixture<OpsFlowApiFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    [Fact]
    public async Task Login_with_valid_manager_credentials_returns_token_and_user()
    {
        var response = await LoginAsync("manager@opsflow.local", "Password123!");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK, got {response.StatusCode}. Authenticate: {string.Join(", ", response.Headers.WwwAuthenticate)}");
        var login = await response.Content.ReadFromJsonAsync<LoginResponseBody>();

        Assert.NotNull(login);
        Assert.False(string.IsNullOrWhiteSpace(login.AccessToken));
        Assert.Equal("Bearer", login.TokenType);
        Assert.True(login.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal("manager@opsflow.local", login.User.Email);
        Assert.False(string.IsNullOrWhiteSpace(login.User.DisplayName));
        Assert.Contains("Manager", login.User.Roles);
    }

    [Fact]
    public async Task Login_with_invalid_password_returns_unauthorized()
    {
        var response = await LoginAsync("manager@opsflow.local", "wrong-password");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData(null, "Password123!")]
    [InlineData("manager@opsflow.local", null)]
    [InlineData("", "Password123!")]
    [InlineData("manager@opsflow.local", "")]
    public async Task Login_with_missing_email_or_password_returns_bad_request(string? email, string? password)
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Me_without_token_returns_unauthorized()
    {
        var response = await _client.GetAsync("/api/auth/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Me_with_valid_token_returns_current_user()
    {
        var loginResponse = await LoginAsync("manager@opsflow.local", "Password123!");
        var login = await loginResponse.Content.ReadFromJsonAsync<LoginResponseBody>();
        Assert.NotNull(login);

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", login.AccessToken);
        var response = await _client.GetAsync("/api/auth/me");

        Assert.True(
            response.StatusCode == HttpStatusCode.OK,
            $"Expected OK, got {response.StatusCode}. Authenticate: {string.Join(", ", response.Headers.WwwAuthenticate)}");
        var currentUser = await response.Content.ReadFromJsonAsync<CurrentUserResponseBody>();

        Assert.NotNull(currentUser);
        Assert.NotEqual(Guid.Empty, currentUser.Id);
        Assert.Equal("manager@opsflow.local", currentUser.Email);
        Assert.False(string.IsNullOrWhiteSpace(currentUser.DisplayName));
        Assert.Contains("Manager", currentUser.Roles);
    }

    [Fact]
    public async Task Health_remains_public()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    private Task<HttpResponseMessage> LoginAsync(string email, string password)
    {
        return _client.PostAsJsonAsync("/api/auth/login", new LoginRequest(email, password));
    }

    private sealed record LoginResponseBody(
        [property: JsonPropertyName("accessToken")] string AccessToken,
        [property: JsonPropertyName("tokenType")] string TokenType,
        [property: JsonPropertyName("expiresAtUtc")] DateTime ExpiresAtUtc,
        [property: JsonPropertyName("user")] UserSummaryBody User);

    private sealed record UserSummaryBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles);

    private sealed record CurrentUserResponseBody(
        [property: JsonPropertyName("id")] Guid Id,
        [property: JsonPropertyName("email")] string Email,
        [property: JsonPropertyName("displayName")] string DisplayName,
        [property: JsonPropertyName("roles")] IReadOnlyList<string> Roles);
}
