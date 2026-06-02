namespace OpsFlow.Application.Cases;

public interface ICaseAssignmentService
{
    Task<CaseDetailDto> AssignCaseAsync(
        Guid caseId,
        AssignCaseRequest? request,
        CancellationToken cancellationToken = default);
}
