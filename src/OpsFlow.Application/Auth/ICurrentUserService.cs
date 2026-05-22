namespace OpsFlow.Application.Auth;

public interface ICurrentUserService
{
    Guid? UserId { get; }
    IReadOnlyCollection<string> Roles { get; }
    bool IsAuthenticated { get; }
    bool IsAnalyst { get; }
    bool IsManagerOrAdmin { get; }
}
