using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Application.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;
using CaseUserSummaryDto = OpsFlow.Application.Cases.UserSummaryDto;

namespace OpsFlow.Infrastructure.Cases;

public sealed class EfCaseStatusService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser,
    IClock clock) : ICaseStatusService
{
    private const int MaxReasonLength = 1000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CaseDetailDto> UpdateStatusAsync(
        Guid caseId,
        UpdateCaseStatusRequest? request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");
        var targetStatus = NormalizeTargetStatus(request?.TargetStatus);
        var reason = NormalizeReason(request?.Reason);
        var requestedRowVersion = NormalizeRowVersion(request?.RowVersion);

        var opsCase = await dbContext.Cases
            .SingleOrDefaultAsync(x => x.Id == caseId, cancellationToken);

        if (opsCase is null)
        {
            throw new CaseNotFoundException(caseId);
        }

        if (!opsCase.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new CaseStatusConcurrencyException("This case was updated by another user. Please refresh.");
        }

        ValidateRoleAccess(opsCase, actorUserId);
        ValidateTransition(opsCase, targetStatus);

        var nowUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var fromStatus = opsCase.Status;
        var action = fromStatus == CaseStatus.Closed && targetStatus == CaseStatus.Reopened
            ? AuditAction.CaseReopened
            : AuditAction.StatusChanged;

        opsCase.Status = targetStatus;
        opsCase.UpdatedAtUtc = nowUtc;
        ApplyStatusTimestamps(opsCase, targetStatus, nowUtc);

        dbContext.Entry(opsCase).Property(x => x.RowVersion).OriginalValue = requestedRowVersion;
        SetNewRowVersionForInMemoryProvider(opsCase);

        dbContext.StatusHistories.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            CaseId = opsCase.Id,
            FromStatus = fromStatus,
            ToStatus = targetStatus,
            ChangedByUserId = actorUserId,
            Reason = reason,
            CreatedAtUtc = nowUtc
        });

        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EntityType = "Case",
            EntityId = opsCase.Id,
            Action = action,
            MetadataJson = JsonSerializer.Serialize(new
            {
                fromStatus = fromStatus.ToString(),
                toStatus = targetStatus.ToString(),
                reason
            }, JsonOptions),
            CreatedAtUtc = nowUtc
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new CaseStatusConcurrencyException("This case was updated by another user. Please refresh.", ex);
        }

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

    private void ValidateRoleAccess(OpsCase opsCase, Guid actorUserId)
    {
        if (currentUser.IsManagerOrAdmin)
        {
            return;
        }

        if (currentUser.IsAnalyst && opsCase.AssignedToUserId == actorUserId)
        {
            return;
        }

        throw new CaseAccessDeniedException("The current user cannot update this case status.");
    }

    private void ValidateTransition(OpsCase opsCase, CaseStatus targetStatus)
    {
        if (opsCase.Status == targetStatus)
        {
            throw new CaseStatusValidationException("Case is already in the requested status.");
        }

        if (targetStatus == CaseStatus.PendingApproval)
        {
            throw new CaseStatusTransitionException("PendingApproval is reserved for the approval workflow.");
        }

        if (currentUser.IsAnalyst)
        {
            ValidateAnalystTransition(opsCase.Status, targetStatus);
            return;
        }

        if (currentUser.IsManagerOrAdmin)
        {
            ValidateManagerTransition(opsCase, targetStatus);
            return;
        }

        throw new CaseAccessDeniedException("The current user cannot update case status.");
    }

    private static void ValidateAnalystTransition(CaseStatus fromStatus, CaseStatus targetStatus)
    {
        var allowed = fromStatus switch
        {
            CaseStatus.Assigned => targetStatus is CaseStatus.InReview or CaseStatus.WaitingInfo,
            CaseStatus.InReview => targetStatus is CaseStatus.WaitingInfo or CaseStatus.Resolved,
            CaseStatus.WaitingInfo => targetStatus is CaseStatus.InReview or CaseStatus.Resolved,
            CaseStatus.Reopened => targetStatus == CaseStatus.InReview,
            _ => false
        };

        if (!allowed)
        {
            throw new CaseStatusTransitionException("The requested status transition is not allowed.");
        }
    }

    private static void ValidateManagerTransition(OpsCase opsCase, CaseStatus targetStatus)
    {
        if (opsCase.Status == CaseStatus.Resolved && targetStatus == CaseStatus.Closed)
        {
            if (opsCase.Priority is CasePriority.High or CasePriority.Critical)
            {
                throw new CaseStatusTransitionException(
                    "High and Critical case closure requires the approval workflow.");
            }

            return;
        }

        var allowed = opsCase.Status switch
        {
            CaseStatus.Assigned => targetStatus is CaseStatus.InReview or CaseStatus.WaitingInfo,
            CaseStatus.InReview => targetStatus is CaseStatus.WaitingInfo or CaseStatus.Resolved,
            CaseStatus.WaitingInfo => targetStatus is CaseStatus.InReview or CaseStatus.Resolved,
            CaseStatus.Closed => targetStatus == CaseStatus.Reopened,
            CaseStatus.Reopened => targetStatus is CaseStatus.InReview or CaseStatus.WaitingInfo,
            _ => false
        };

        if (!allowed)
        {
            throw new CaseStatusTransitionException("The requested status transition is not allowed.");
        }
    }

    private static CaseStatus NormalizeTargetStatus(string? targetStatus)
    {
        if (string.IsNullOrWhiteSpace(targetStatus))
        {
            throw new CaseStatusValidationException("TargetStatus is required.");
        }

        var normalized = targetStatus.Trim();
        if (normalized.All(char.IsDigit) ||
            !Enum.TryParse<CaseStatus>(normalized, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new CaseStatusValidationException("Invalid targetStatus.");
        }

        return parsed;
    }

    private static string NormalizeReason(string? reason)
    {
        if (reason is null)
        {
            throw new CaseStatusValidationException("Reason is required.");
        }

        var normalized = reason.Trim();
        if (normalized.Length == 0)
        {
            throw new CaseStatusValidationException("Reason is required.");
        }

        if (normalized.Length > MaxReasonLength)
        {
            throw new CaseStatusValidationException($"Reason must be {MaxReasonLength} characters or fewer.");
        }

        return normalized;
    }

    private static byte[] NormalizeRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new CaseStatusValidationException("RowVersion is required.");
        }

        try
        {
            return Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new CaseStatusValidationException("Invalid rowVersion.");
        }
    }

    private static void ApplyStatusTimestamps(OpsCase opsCase, CaseStatus targetStatus, DateTime nowUtc)
    {
        if (targetStatus == CaseStatus.Resolved)
        {
            opsCase.ResolvedAtUtc ??= nowUtc;
        }

        if (targetStatus == CaseStatus.Closed)
        {
            opsCase.ClosedAtUtc = nowUtc;
        }
        else if (targetStatus == CaseStatus.Reopened)
        {
            opsCase.ClosedAtUtc = null;
        }
    }

    private void SetNewRowVersionForInMemoryProvider(OpsCase opsCase)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            opsCase.RowVersion = Guid.NewGuid().ToByteArray();
        }
    }
}
