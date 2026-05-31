namespace OpsFlow.Application.Cases;

public interface ICaseCommandService
{
    Task<CaseDetailDto> CreateCaseAsync(CreateCaseRequest request, CancellationToken cancellationToken = default);
}
