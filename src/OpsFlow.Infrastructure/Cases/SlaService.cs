using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Cases;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Cases;

public sealed class SlaService(OpsFlowDbContext dbContext) : ISlaService
{
    public async Task<DateTime> CalculateDueAtUtcAsync(
        Guid caseTypeId,
        CasePriority priority,
        DateTime createdAtUtc,
        CancellationToken cancellationToken = default)
    {
        var caseTypeExists = await dbContext.CaseTypes
            .AnyAsync(x => x.Id == caseTypeId, cancellationToken);

        if (!caseTypeExists)
        {
            throw new CaseTypeNotFoundException("Case type was not found.");
        }

        var targetHours = await dbContext.SlaRules
            .Where(x => x.CaseTypeId == caseTypeId && x.Priority == priority && x.IsActive)
            .Select(x => (int?)x.TargetHours)
            .SingleOrDefaultAsync(cancellationToken);

        if (targetHours is null)
        {
            throw new SlaRuleNotFoundException("Active SLA rule was not found.");
        }

        return DateTime.SpecifyKind(createdAtUtc, DateTimeKind.Utc).AddHours(targetHours.Value);
    }
}
