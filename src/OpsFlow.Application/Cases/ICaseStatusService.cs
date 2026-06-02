namespace OpsFlow.Application.Cases;

public interface ICaseStatusService
{
    Task<CaseDetailDto> UpdateStatusAsync(
        Guid caseId,
        UpdateCaseStatusRequest? request,
        CancellationToken cancellationToken = default);
}
