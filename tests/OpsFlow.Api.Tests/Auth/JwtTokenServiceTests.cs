using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.Extensions.Options;
using OpsFlow.Domain.Entities;
using OpsFlow.Infrastructure.Auth;

namespace OpsFlow.Api.Tests.Auth;

public sealed class JwtTokenServiceTests
{
    [Fact]
    public void CreateToken_includes_required_claims_and_expiration()
    {
        var service = new JwtTokenService(Options.Create(new JwtOptions
        {
            Issuer = OpsFlowApiFactory.JwtIssuer,
            Audience = OpsFlowApiFactory.JwtAudience,
            SigningKey = OpsFlowApiFactory.JwtSigningKey,
            AccessTokenMinutes = 30
        }));
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            Email = "manager@opsflow.local",
            UserName = "manager@opsflow.local",
            DisplayName = "Morgan Manager",
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        var token = service.CreateToken(user, ["Manager"]);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token.AccessToken);

        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Sub && claim.Value == user.Id.ToString());
        Assert.Contains(jwt.Claims, claim => claim.Type == JwtRegisteredClaimNames.Email && claim.Value == user.Email);
        Assert.Contains(jwt.Claims, claim => claim.Type == ClaimTypes.Role && claim.Value == "Manager");
        Assert.True(token.ExpiresAtUtc > DateTime.UtcNow);
        Assert.Equal(token.ExpiresAtUtc, jwt.ValidTo, TimeSpan.FromSeconds(1));
    }
}
