namespace OpsFlow.Application.Cases;

public sealed record CaseNoteDto(
    Guid Id,
    string Body,
    UserSummaryDto CreatedBy,
    DateTime CreatedAtUtc);
