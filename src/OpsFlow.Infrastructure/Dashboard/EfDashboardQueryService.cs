using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Application.Common;
using OpsFlow.Application.Dashboard;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;

namespace OpsFlow.Infrastructure.Dashboard;

public sealed class EfDashboardQueryService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser,
    IClock clock) : IDashboardQueryService
{
    public async Task<DashboardSummaryDto> GetSummaryAsync(CancellationToken cancellationToken = default)
    {
        var nowUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var scopedCases = ApplyAccessScope(dbContext.Cases.AsNoTracking());
        var openCasesQuery = scopedCases.Where(x => x.Status != CaseStatus.Closed);

        var openCases = await openCasesQuery.CountAsync(cancellationToken);
        var overdueOpenCases = await openCasesQuery
            .CountAsync(x => x.DueAtUtc < nowUtc, cancellationToken);
        var pendingApprovals = await ApplyApprovalAccessScope(dbContext.ApprovalRequests.AsNoTracking())
            .CountAsync(x => x.Status == ApprovalStatus.Pending, cancellationToken);

        var openCreatedAtValues = await openCasesQuery
            .Select(x => x.CreatedAtUtc)
            .ToListAsync(cancellationToken);
        var averageOpenAgeHours = openCreatedAtValues.Count == 0
            ? 0
            : openCreatedAtValues.Average(createdAtUtc => (nowUtc - createdAtUtc).TotalHours);
        var slaBreachRate = openCases == 0 ? 0 : overdueOpenCases / (double)openCases;

        return new DashboardSummaryDto(
            openCases,
            overdueOpenCases,
            pendingApprovals,
            averageOpenAgeHours,
            slaBreachRate);
    }

    public async Task<DashboardBreakdownsDto> GetBreakdownsAsync(CancellationToken cancellationToken = default)
    {
        var scopedCases = ApplyAccessScope(dbContext.Cases.AsNoTracking());

        var byStatus = await scopedCases
            .GroupBy(x => x.Status)
            .Select(x => new { Status = x.Key, Count = x.Count() })
            .OrderBy(x => x.Status)
            .ToListAsync(cancellationToken);

        var byPriority = await scopedCases
            .GroupBy(x => x.Priority)
            .Select(x => new { Priority = x.Key, Count = x.Count() })
            .OrderBy(x => x.Priority)
            .ToListAsync(cancellationToken);

        var byCaseType = await scopedCases
            .GroupBy(x => new { x.CaseTypeId, x.CaseType.Name })
            .Select(x => new { x.Key.CaseTypeId, x.Key.Name, Count = x.Count() })
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);

        var byAssignee = await scopedCases
            .GroupBy(x => new
            {
                x.AssignedToUserId,
                DisplayName = x.AssignedToUser == null ? null : x.AssignedToUser.DisplayName
            })
            .Select(x => new
            {
                x.Key.AssignedToUserId,
                x.Key.DisplayName,
                Count = x.Count()
            })
            .OrderBy(x => x.DisplayName ?? "Unassigned")
            .ToListAsync(cancellationToken);

        return new DashboardBreakdownsDto(
            byStatus.Select(x => new DashboardBreakdownItemDto(
                x.Status.ToString(),
                x.Status.ToString(),
                x.Count,
                new Dictionary<string, string> { ["status"] = x.Status.ToString() })).ToList(),
            byPriority.Select(x => new DashboardBreakdownItemDto(
                x.Priority.ToString(),
                x.Priority.ToString(),
                x.Count,
                new Dictionary<string, string> { ["priority"] = x.Priority.ToString() })).ToList(),
            byCaseType.Select(x => new DashboardBreakdownItemDto(
                x.CaseTypeId.ToString(),
                x.Name,
                x.Count,
                new Dictionary<string, string> { ["caseTypeId"] = x.CaseTypeId.ToString() })).ToList(),
            byAssignee.Select(x => new DashboardBreakdownItemDto(
                x.AssignedToUserId?.ToString() ?? "unassigned",
                x.DisplayName ?? "Unassigned",
                x.Count,
                x.AssignedToUserId is null
                    ? null
                    : new Dictionary<string, string> { ["assignedToUserId"] = x.AssignedToUserId.Value.ToString() })).ToList());
    }

    private IQueryable<OpsCase> ApplyAccessScope(IQueryable<OpsCase> cases)
    {
        var userId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");

        if (currentUser.IsManagerOrAdmin)
        {
            return cases;
        }

        if (currentUser.IsAnalyst)
        {
            return cases.Where(x => x.AssignedToUserId == userId);
        }

        throw new CaseAccessDeniedException("The current user cannot access dashboard data.");
    }

    private IQueryable<ApprovalRequest> ApplyApprovalAccessScope(IQueryable<ApprovalRequest> approvals)
    {
        var userId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");

        if (currentUser.IsManagerOrAdmin)
        {
            return approvals;
        }

        if (currentUser.IsAnalyst)
        {
            return approvals.Where(x => x.Case.AssignedToUserId == userId);
        }

        throw new CaseAccessDeniedException("The current user cannot access dashboard data.");
    }
}
