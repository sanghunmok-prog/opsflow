namespace OpsFlow.Application.Auth;

public sealed record UserSummaryDto(
    Guid Id,
    string Email,
    string DisplayName,
    IReadOnlyList<string> Roles);
