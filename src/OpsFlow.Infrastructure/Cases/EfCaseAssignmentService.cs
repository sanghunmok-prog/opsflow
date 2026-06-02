using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Application.Common;
using OpsFlow.Domain.Constants;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;
using CaseUserSummaryDto = OpsFlow.Application.Cases.UserSummaryDto;

namespace OpsFlow.Infrastructure.Cases;

public sealed class EfCaseAssignmentService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser,
    IClock clock) : ICaseAssignmentService
{
    private const int MaxReasonLength = 500;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CaseDetailDto> AssignCaseAsync(
        Guid caseId,
        AssignCaseRequest? request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");
        var assignedToUserId = NormalizeAssignedToUserId(request?.AssignedToUserId);
        var reason = NormalizeReason(request?.Reason);

        var opsCase = await dbContext.Cases
            .SingleOrDefaultAsync(x => x.Id == caseId, cancellationToken);

        if (opsCase is null)
        {
            throw new CaseNotFoundException(caseId);
        }

        if (opsCase.Status == CaseStatus.Closed)
        {
            throw new CaseAssignmentValidationException("Closed cases cannot be assigned.");
        }

        if (opsCase.AssignedToUserId == assignedToUserId)
        {
            throw new CaseAssignmentValidationException("Case is already assigned to the selected analyst.");
        }

        var targetIsActiveAnalyst = await dbContext.Users
            .Where(user => user.Id == assignedToUserId && user.IsActive)
            .Join(
                dbContext.UserRoles,
                user => user.Id,
                userRole => userRole.UserId,
                (user, userRole) => new { user, userRole })
            .Join(
                dbContext.Roles.Where(role => role.Name == OpsFlowRoles.Analyst),
                userAndRole => userAndRole.userRole.RoleId,
                role => role.Id,
                (userAndRole, _) => userAndRole.user.Id)
            .AnyAsync(cancellationToken);

        if (!targetIsActiveAnalyst)
        {
            throw new CaseAssignmentValidationException("Assignment target must be an active Analyst.");
        }

        var nowUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var fromUserId = opsCase.AssignedToUserId;
        var previousStatus = opsCase.Status;

        opsCase.AssignedToUserId = assignedToUserId;
        opsCase.UpdatedAtUtc = nowUtc;

        dbContext.AssignmentHistories.Add(new AssignmentHistory
        {
            Id = Guid.NewGuid(),
            CaseId = opsCase.Id,
            FromUserId = fromUserId,
            ToUserId = assignedToUserId,
            AssignedByUserId = actorUserId,
            Reason = reason,
            CreatedAtUtc = nowUtc
        });

        if (previousStatus == CaseStatus.New)
        {
            opsCase.Status = CaseStatus.Assigned;
            dbContext.StatusHistories.Add(new StatusHistory
            {
                Id = Guid.NewGuid(),
                CaseId = opsCase.Id,
                FromStatus = CaseStatus.New,
                ToStatus = CaseStatus.Assigned,
                ChangedByUserId = actorUserId,
                Reason = reason,
                CreatedAtUtc = nowUtc
            });
        }

        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EntityType = "Case",
            EntityId = opsCase.Id,
            Action = AuditAction.Assigned,
            MetadataJson = JsonSerializer.Serialize(new
            {
                fromUserId,
                toUserId = assignedToUserId,
                reason
            }, JsonOptions),
            CreatedAtUtc = nowUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == opsCase.Id)
            .Select(x => new CaseDetailDto(
                x.Id,
                x.CaseNumber,
                x.Title,
                x.Description,
                new CaseTypeSummaryDto(x.CaseType.Id, x.CaseType.Name),
                x.Priority.ToString(),
                x.Status.ToString(),
                x.AssignedToUser == null
                    ? null
                    : new CaseUserSummaryDto(x.AssignedToUser.Id, x.AssignedToUser.DisplayName),
                new CaseUserSummaryDto(x.CreatedByUser.Id, x.CreatedByUser.DisplayName),
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.DueAtUtc,
                x.Status != CaseStatus.Closed && nowUtc > x.DueAtUtc,
                Convert.ToBase64String(x.RowVersion)))
            .SingleAsync(cancellationToken);
    }

    private static Guid NormalizeAssignedToUserId(Guid? assignedToUserId)
    {
        if (assignedToUserId is null || assignedToUserId == Guid.Empty)
        {
            throw new CaseAssignmentValidationException("AssignedToUserId is required.");
        }

        return assignedToUserId.Value;
    }

    private static string NormalizeReason(string? reason)
    {
        if (reason is null)
        {
            throw new CaseAssignmentValidationException("Reason is required.");
        }

        var normalized = reason.Trim();
        if (normalized.Length == 0)
        {
            throw new CaseAssignmentValidationException("Reason is required.");
        }

        if (normalized.Length > MaxReasonLength)
        {
            throw new CaseAssignmentValidationException($"Reason must be {MaxReasonLength} characters or fewer.");
        }

        return normalized;
    }
}
