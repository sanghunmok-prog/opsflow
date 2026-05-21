namespace OpsFlow.Domain.Constants;

public static class OpsFlowRoles
{
    public const string Analyst = "Analyst";
    public const string Manager = "Manager";
    public const string Admin = "Admin";

    public static readonly string[] All = [Analyst, Manager, Admin];
}
