using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Cases;

public sealed class EfCaseAccessService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser) : ICaseAccessService
{
    public async Task<CaseAccessStatus> GetAccessStatusAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var assignedToUserId = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == caseId)
            .Select(x => x.AssignedToUserId)
            .SingleOrDefaultAsync(cancellationToken);

        if (assignedToUserId is null)
        {
            var exists = await dbContext.Cases
                .AsNoTracking()
                .AnyAsync(x => x.Id == caseId, cancellationToken);

            if (!exists)
            {
                return CaseAccessStatus.NotFound;
            }
        }

        if (currentUser.UserId is not { } userId)
        {
            return CaseAccessStatus.Forbidden;
        }

        if (currentUser.IsManagerOrAdmin ||
            (currentUser.IsAnalyst && assignedToUserId == userId))
        {
            return CaseAccessStatus.Allowed;
        }

        return CaseAccessStatus.Forbidden;
    }
}
