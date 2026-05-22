namespace OpsFlow.Application.Auth;

public sealed record LoginResponse(
    string AccessToken,
    string TokenType,
    DateTime ExpiresAtUtc,
    UserSummaryDto User);
