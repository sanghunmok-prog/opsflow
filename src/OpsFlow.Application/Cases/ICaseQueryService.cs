namespace OpsFlow.Application.Cases;

public interface ICaseQueryService
{
    Task<PagedResult<CaseListItemDto>> GetCasesAsync(CaseListQuery query, CancellationToken cancellationToken = default);
    Task<CaseDetailDto?> GetCaseAsync(Guid id, CancellationToken cancellationToken = default);
}
