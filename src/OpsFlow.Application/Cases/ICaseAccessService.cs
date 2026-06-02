namespace OpsFlow.Application.Cases;

public interface ICaseAccessService
{
    Task<CaseAccessStatus> GetAccessStatusAsync(Guid caseId, CancellationToken cancellationToken = default);
}
