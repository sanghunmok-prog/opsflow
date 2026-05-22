namespace OpsFlow.Application.Cases;

public sealed record CaseListItemDto(
    Guid Id,
    string CaseNumber,
    string Title,
    CaseTypeSummaryDto CaseType,
    string Priority,
    string Status,
    UserSummaryDto? AssignedTo,
    DateTime CreatedAtUtc,
    DateTime DueAtUtc);
