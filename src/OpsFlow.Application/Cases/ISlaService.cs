using OpsFlow.Domain.Enums;

namespace OpsFlow.Application.Cases;

public interface ISlaService
{
    Task<DateTime> CalculateDueAtUtcAsync(
        Guid caseTypeId,
        CasePriority priority,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default);
}
