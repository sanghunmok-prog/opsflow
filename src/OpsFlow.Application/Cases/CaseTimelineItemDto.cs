namespace OpsFlow.Application.Cases;

public sealed record CaseTimelineItemDto(
    Guid Id,
    string Action,
    UserSummaryDto? Actor,
    DateTime CreatedAtUtc,
    string Description);
