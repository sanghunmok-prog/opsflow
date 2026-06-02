namespace OpsFlow.Application.Users;

public sealed record AnalystLookupDto(
    Guid Id,
    string DisplayName,
    string Email);
