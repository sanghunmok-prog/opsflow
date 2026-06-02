namespace OpsFlow.Application.Cases;

public interface ICaseTimelineService
{
    Task<IReadOnlyList<CaseTimelineItemDto>?> GetTimelineAsync(Guid caseId, CancellationToken cancellationToken = default);
}
