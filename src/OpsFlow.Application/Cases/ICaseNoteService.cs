namespace OpsFlow.Application.Cases;

public interface ICaseNoteService
{
    Task<IReadOnlyList<CaseNoteDto>?> GetNotesAsync(Guid caseId, CancellationToken cancellationToken = default);
    Task<CaseNoteDto?> AddNoteAsync(Guid caseId, CreateCaseNoteRequest? request, CancellationToken cancellationToken = default);
}
