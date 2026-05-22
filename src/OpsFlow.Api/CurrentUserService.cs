using System.Security.Claims;
using OpsFlow.Application.Auth;
using OpsFlow.Domain.Constants;

namespace OpsFlow.Api;

public sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor) : ICurrentUserService
{
    public Guid? UserId
    {
        get
        {
            var user = httpContextAccessor.HttpContext?.User;
            var userIdValue = user?.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? user?.FindFirstValue(System.IdentityModel.Tokens.Jwt.JwtRegisteredClaimNames.Sub);

            return Guid.TryParse(userIdValue, out var userId) ? userId : null;
        }
    }

    public IReadOnlyCollection<string> Roles =>
        httpContextAccessor.HttpContext?.User.FindAll(ClaimTypes.Role).Select(x => x.Value).ToArray()
        ?? [];

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated == true;

    public bool IsAnalyst => Roles.Contains(OpsFlowRoles.Analyst);

    public bool IsManagerOrAdmin =>
        Roles.Contains(OpsFlowRoles.Manager) || Roles.Contains(OpsFlowRoles.Admin);
}
