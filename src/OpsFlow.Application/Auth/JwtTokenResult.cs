namespace OpsFlow.Application.Auth;

public sealed record JwtTokenResult(
    string AccessToken,
    DateTime ExpiresAtUtc);
