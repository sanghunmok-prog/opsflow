using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using OpsFlow.Application.Auth;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api.Tests.Auth;

public sealed class AuthorizationPolicyTests(OpsFlowApiFactory factory) : IClassFixture<OpsFlowApiFactory>
{
    [Theory]
    [InlineData(OpsFlowRoles.Manager)]
    [InlineData(OpsFlowRoles.Admin)]
    public async Task RequireManagerOrAdmin_allows_manager_and_admin(string role)
    {
        var authorizationService = factory.Services.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(role);

        var result = await authorizationService.AuthorizeAsync(user, null, OpsFlowPolicies.RequireManagerOrAdmin);

        Assert.True(result.Succeeded);
    }

    [Fact]
    public async Task RequireManagerOrAdmin_rejects_analyst()
    {
        var authorizationService = factory.Services.GetRequiredService<IAuthorizationService>();
        var user = CreateUser(OpsFlowRoles.Analyst);

        var result = await authorizationService.AuthorizeAsync(user, null, OpsFlowPolicies.RequireManagerOrAdmin);

        Assert.False(result.Succeeded);
    }

    private static ClaimsPrincipal CreateUser(string role)
    {
        return new ClaimsPrincipal(new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.Role, role)
            ],
            "Test"));
    }
}
