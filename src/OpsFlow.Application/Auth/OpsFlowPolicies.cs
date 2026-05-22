namespace OpsFlow.Application.Auth;

public static class OpsFlowPolicies
{
    public const string RequireAdmin = "RequireAdmin";
    public const string RequireManagerOrAdmin = "RequireManagerOrAdmin";
    public const string RequireAnalystOrManagerOrAdmin = "RequireAnalystOrManagerOrAdmin";
    public const string RequireAuthenticatedUser = "RequireAuthenticatedUser";
}
