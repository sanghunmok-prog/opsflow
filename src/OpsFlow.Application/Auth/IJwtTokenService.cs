using OpsFlow.Domain.Entities;

namespace OpsFlow.Application.Auth;

public interface IJwtTokenService
{
    JwtTokenResult CreateToken(AppUser user, IReadOnlyCollection<string> roles);
}
