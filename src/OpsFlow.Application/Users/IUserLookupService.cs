namespace OpsFlow.Application.Users;

public interface IUserLookupService
{
    Task<IReadOnlyList<AnalystLookupDto>> GetActiveAnalystsAsync(CancellationToken cancellationToken = default);
}
