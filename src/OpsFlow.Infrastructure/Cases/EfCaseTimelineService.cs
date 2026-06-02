using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Cases;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;
using CaseUserSummaryDto = OpsFlow.Application.Cases.UserSummaryDto;

namespace OpsFlow.Infrastructure.Cases;

public sealed class EfCaseTimelineService(
    OpsFlowDbContext dbContext,
    ICaseAccessService caseAccessService) : ICaseTimelineService
{
    public async Task<IReadOnlyList<CaseTimelineItemDto>?> GetTimelineAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        var access = await caseAccessService.GetAccessStatusAsync(caseId, cancellationToken);
        if (access == CaseAccessStatus.NotFound)
        {
            return null;
        }

        if (access == CaseAccessStatus.Forbidden)
        {
            throw new CaseAccessDeniedException("The current user cannot access this case.");
        }

        return await dbContext.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.EntityType == "Case" &&
                x.EntityId == caseId &&
                (x.Action == AuditAction.CaseCreated || x.Action == AuditAction.NoteAdded))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new CaseTimelineItemDto(
                x.Id,
                x.Action.ToString(),
                x.ActorUser == null
                    ? null
                    : new CaseUserSummaryDto(x.ActorUser.Id, x.ActorUser.DisplayName),
                x.CreatedAtUtc,
                x.Action == AuditAction.CaseCreated ? "Case created" : "Note added"))
            .ToListAsync(cancellationToken);
    }
}
