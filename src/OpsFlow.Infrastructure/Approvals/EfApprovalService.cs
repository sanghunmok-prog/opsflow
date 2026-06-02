using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Approvals;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Application.Common;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;
using CaseUserSummaryDto = OpsFlow.Application.Cases.UserSummaryDto;

namespace OpsFlow.Infrastructure.Approvals;

public sealed class EfApprovalService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser,
    IClock clock) : IApprovalService
{
    private const int MaxReasonLength = 1000;
    private const int MaxPageSize = 100;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<ApprovalRequestDto> RequestClosureAsync(
        Guid caseId,
        RequestClosureRequest? request,
        CancellationToken cancellationToken = default)
    {
        var actorUserId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");
        var requestReason = NormalizeRequiredReason(request?.RequestReason, "requestReason");
        var requestedRowVersion = NormalizeRequiredRowVersion(request?.RowVersion);

        var opsCase = await dbContext.Cases
            .SingleOrDefaultAsync(x => x.Id == caseId, cancellationToken);

        if (opsCase is null)
        {
            throw new CaseNotFoundException(caseId);
        }

        if (!opsCase.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ApprovalConcurrencyException("This case was updated by another user. Please refresh.");
        }

        ValidateCaseAccess(opsCase, actorUserId);

        var hasPendingApproval = await dbContext.ApprovalRequests
            .AnyAsync(x => x.CaseId == caseId && x.Status == ApprovalStatus.Pending, cancellationToken);
        if (hasPendingApproval)
        {
            throw new ApprovalStateConflictException("This case already has a pending approval request.");
        }

        ValidateClosureRequestState(opsCase);

        var nowUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var approvalRequest = new ApprovalRequest
        {
            Id = Guid.NewGuid(),
            CaseId = opsCase.Id,
            RequestedByUserId = actorUserId,
            ReviewedByUserId = null,
            Status = ApprovalStatus.Pending,
            RequestReason = requestReason,
            DecisionReason = null,
            RequestedAtUtc = nowUtc,
            DecisionAtUtc = null
        };

        opsCase.Status = CaseStatus.PendingApproval;
        opsCase.UpdatedAtUtc = nowUtc;
        dbContext.Entry(opsCase).Property(x => x.RowVersion).OriginalValue = requestedRowVersion;
        SetNewRowVersionForInMemoryProvider(opsCase);

        dbContext.ApprovalRequests.Add(approvalRequest);
        dbContext.StatusHistories.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            CaseId = opsCase.Id,
            FromStatus = CaseStatus.Resolved,
            ToStatus = CaseStatus.PendingApproval,
            ChangedByUserId = actorUserId,
            Reason = requestReason,
            CreatedAtUtc = nowUtc
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = actorUserId,
            EntityType = "Case",
            EntityId = opsCase.Id,
            Action = AuditAction.ClosureRequested,
            MetadataJson = JsonSerializer.Serialize(new
            {
                fromStatus = CaseStatus.Resolved.ToString(),
                toStatus = CaseStatus.PendingApproval.ToString(),
                requestReason,
                approvalRequestId = approvalRequest.Id
            }, JsonOptions),
            CreatedAtUtc = nowUtc
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ApprovalConcurrencyException("This case was updated by another user. Please refresh.", ex);
        }

        return await BuildApprovalRequestDtoQuery(approvalRequest.Id)
            .SingleAsync(cancellationToken);
    }

    public async Task<PagedResult<ApprovalQueueItemDto>> GetPendingApprovalsAsync(
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(page, pageSize);
        ValidateManagerOrAdmin();

        var nowUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var approvals = dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.Status == ApprovalStatus.Pending)
            .OrderBy(x => x.RequestedAtUtc)
            .ThenBy(x => x.Id);

        var totalCount = await approvals.CountAsync(cancellationToken);
        var items = await approvals
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(x => new ApprovalQueueItemDto(
                x.Id,
                x.CaseId,
                x.Case.CaseNumber,
                x.Case.Title,
                x.Case.Priority.ToString(),
                x.Case.Status.ToString(),
                x.RequestReason,
                new CaseUserSummaryDto(x.RequestedByUser.Id, x.RequestedByUser.DisplayName),
                x.RequestedAtUtc,
                x.Case.AssignedToUser == null
                    ? null
                    : new CaseUserSummaryDto(x.Case.AssignedToUser.Id, x.Case.AssignedToUser.DisplayName),
                x.Case.DueAtUtc,
                x.Case.Status != CaseStatus.Closed && nowUtc > x.Case.DueAtUtc,
                Convert.ToBase64String(x.Case.RowVersion)))
            .ToListAsync(cancellationToken);

        return new PagedResult<ApprovalQueueItemDto>(
            items,
            page,
            pageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)pageSize));
    }

    public Task<ApprovalDecisionResultDto> ApproveAsync(
        Guid approvalId,
        ApprovalDecisionRequest? request,
        CancellationToken cancellationToken = default)
    {
        return DecideAsync(
            approvalId,
            request,
            ApprovalStatus.Approved,
            CaseStatus.Closed,
            AuditAction.ApprovalApproved,
            requireDecisionReason: false,
            defaultReason: "Approved for closure.",
            cancellationToken);
    }

    public Task<ApprovalDecisionResultDto> RejectAsync(
        Guid approvalId,
        ApprovalDecisionRequest? request,
        CancellationToken cancellationToken = default)
    {
        return DecideAsync(
            approvalId,
            request,
            ApprovalStatus.Rejected,
            CaseStatus.InReview,
            AuditAction.ApprovalRejected,
            requireDecisionReason: true,
            defaultReason: null,
            cancellationToken);
    }

    private async Task<ApprovalDecisionResultDto> DecideAsync(
        Guid approvalId,
        ApprovalDecisionRequest? request,
        ApprovalStatus approvalStatus,
        CaseStatus targetStatus,
        AuditAction auditAction,
        bool requireDecisionReason,
        string? defaultReason,
        CancellationToken cancellationToken)
    {
        var reviewerUserId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");
        ValidateManagerOrAdmin();
        var decisionReason = requireDecisionReason
            ? NormalizeRequiredReason(request?.DecisionReason, "decisionReason")
            : NormalizeOptionalReason(request?.DecisionReason, "decisionReason");
        var requestedRowVersion = NormalizeOptionalRowVersion(request?.RowVersion);

        var approval = await dbContext.ApprovalRequests
            .Include(x => x.Case)
            .SingleOrDefaultAsync(x => x.Id == approvalId, cancellationToken);

        if (approval is null)
        {
            throw new ApprovalNotFoundException(approvalId);
        }

        if (approval.Status != ApprovalStatus.Pending)
        {
            throw new ApprovalStateConflictException("This approval request has already been decided.");
        }

        var opsCase = approval.Case;
        if (requestedRowVersion is not null && !opsCase.RowVersion.SequenceEqual(requestedRowVersion))
        {
            throw new ApprovalConcurrencyException("This case was updated by another user. Please refresh.");
        }

        if (opsCase.Status != CaseStatus.PendingApproval)
        {
            throw new ApprovalStateConflictException("The related case is not pending approval.");
        }

        if (opsCase.Priority is not (CasePriority.High or CasePriority.Critical))
        {
            throw new ApprovalValidationException("Only High and Critical cases use closure approvals.");
        }

        var nowUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var historyReason = decisionReason ?? defaultReason;

        approval.Status = approvalStatus;
        approval.ReviewedByUserId = reviewerUserId;
        approval.DecisionReason = decisionReason;
        approval.DecisionAtUtc = nowUtc;

        opsCase.Status = targetStatus;
        opsCase.UpdatedAtUtc = nowUtc;
        if (targetStatus == CaseStatus.Closed)
        {
            opsCase.ClosedAtUtc = nowUtc;
        }

        if (requestedRowVersion is not null)
        {
            dbContext.Entry(opsCase).Property(x => x.RowVersion).OriginalValue = requestedRowVersion;
        }

        SetNewRowVersionForInMemoryProvider(opsCase);

        dbContext.StatusHistories.Add(new StatusHistory
        {
            Id = Guid.NewGuid(),
            CaseId = opsCase.Id,
            FromStatus = CaseStatus.PendingApproval,
            ToStatus = targetStatus,
            ChangedByUserId = reviewerUserId,
            Reason = historyReason,
            CreatedAtUtc = nowUtc
        });
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = reviewerUserId,
            EntityType = "Case",
            EntityId = opsCase.Id,
            Action = auditAction,
            MetadataJson = JsonSerializer.Serialize(new
            {
                approvalRequestId = approval.Id,
                fromStatus = CaseStatus.PendingApproval.ToString(),
                toStatus = targetStatus.ToString(),
                decisionReason
            }, JsonOptions),
            CreatedAtUtc = nowUtc
        });

        try
        {
            await dbContext.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ApprovalConcurrencyException("This case was updated by another user. Please refresh.", ex);
        }

        return await BuildApprovalDecisionDtoQuery(approval.Id)
            .SingleAsync(cancellationToken);
    }

    private void ValidateCaseAccess(OpsCase opsCase, Guid actorUserId)
    {
        if (currentUser.IsManagerOrAdmin)
        {
            return;
        }

        if (currentUser.IsAnalyst && opsCase.AssignedToUserId == actorUserId)
        {
            return;
        }

        throw new CaseAccessDeniedException("The current user cannot request closure for this case.");
    }

    private static void ValidateClosureRequestState(OpsCase opsCase)
    {
        if (opsCase.Status == CaseStatus.Closed)
        {
            throw new ApprovalValidationException("Closed cases cannot request closure approval.");
        }

        if (opsCase.Status != CaseStatus.Resolved)
        {
            throw new ApprovalValidationException("Only resolved cases can request closure approval.");
        }

        if (opsCase.Priority is not (CasePriority.High or CasePriority.Critical))
        {
            throw new ApprovalValidationException("Low and Medium cases close through the normal status workflow.");
        }
    }

    private void ValidateManagerOrAdmin()
    {
        if (!currentUser.IsManagerOrAdmin)
        {
            throw new CaseAccessDeniedException("The current user cannot manage approvals.");
        }
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new ApprovalValidationException("page must be greater than or equal to 1.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            throw new ApprovalValidationException($"pageSize must be between 1 and {MaxPageSize}.");
        }
    }

    private static string NormalizeRequiredReason(string? reason, string fieldName)
    {
        if (reason is null)
        {
            throw new ApprovalValidationException($"{fieldName} is required.");
        }

        var normalized = reason.Trim();
        if (normalized.Length == 0)
        {
            throw new ApprovalValidationException($"{fieldName} is required.");
        }

        if (normalized.Length > MaxReasonLength)
        {
            throw new ApprovalValidationException($"{fieldName} must be {MaxReasonLength} characters or fewer.");
        }

        return normalized;
    }

    private static string? NormalizeOptionalReason(string? reason, string fieldName)
    {
        if (reason is null)
        {
            return null;
        }

        var normalized = reason.Trim();
        if (normalized.Length == 0)
        {
            return null;
        }

        if (normalized.Length > MaxReasonLength)
        {
            throw new ApprovalValidationException($"{fieldName} must be {MaxReasonLength} characters or fewer.");
        }

        return normalized;
    }

    private static byte[] NormalizeRequiredRowVersion(string? rowVersion)
    {
        if (string.IsNullOrWhiteSpace(rowVersion))
        {
            throw new ApprovalValidationException("rowVersion is required.");
        }

        return DecodeRowVersion(rowVersion);
    }

    private static byte[]? NormalizeOptionalRowVersion(string? rowVersion)
    {
        return string.IsNullOrWhiteSpace(rowVersion) ? null : DecodeRowVersion(rowVersion);
    }

    private static byte[] DecodeRowVersion(string rowVersion)
    {
        try
        {
            return Convert.FromBase64String(rowVersion);
        }
        catch (FormatException)
        {
            throw new ApprovalValidationException("Invalid rowVersion.");
        }
    }

    private IQueryable<ApprovalRequestDto> BuildApprovalRequestDtoQuery(Guid approvalId)
    {
        return dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.Id == approvalId)
            .Select(x => new ApprovalRequestDto(
                x.Id,
                x.CaseId,
                x.Case.CaseNumber,
                x.Case.Title,
                x.Case.Priority.ToString(),
                x.Case.Status.ToString(),
                x.Status.ToString(),
                x.RequestReason,
                new CaseUserSummaryDto(x.RequestedByUser.Id, x.RequestedByUser.DisplayName),
                x.RequestedAtUtc,
                Convert.ToBase64String(x.Case.RowVersion)));
    }

    private IQueryable<ApprovalDecisionResultDto> BuildApprovalDecisionDtoQuery(Guid approvalId)
    {
        return dbContext.ApprovalRequests
            .AsNoTracking()
            .Where(x => x.Id == approvalId)
            .Select(x => new ApprovalDecisionResultDto(
                x.Id,
                x.CaseId,
                x.Case.CaseNumber,
                x.Case.Title,
                x.Status.ToString(),
                x.Case.Status.ToString(),
                x.DecisionReason,
                new CaseUserSummaryDto(x.ReviewedByUser!.Id, x.ReviewedByUser.DisplayName),
                x.DecisionAtUtc!.Value,
                Convert.ToBase64String(x.Case.RowVersion)));
    }

    private void SetNewRowVersionForInMemoryProvider(OpsCase opsCase)
    {
        if (dbContext.Database.ProviderName == "Microsoft.EntityFrameworkCore.InMemory")
        {
            opsCase.RowVersion = Guid.NewGuid().ToByteArray();
        }
    }
}
