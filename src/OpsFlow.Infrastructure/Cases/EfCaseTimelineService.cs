using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Cases;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;
using CaseUserSummaryDto = OpsFlow.Application.Cases.UserSummaryDto;
using System.Text.Json;

namespace OpsFlow.Infrastructure.Cases;

public sealed class EfCaseTimelineService(
    OpsFlowDbContext dbContext,
    ICaseAccessService caseAccessService) : ICaseTimelineService
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

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

        var auditLogs = await dbContext.AuditLogs
            .AsNoTracking()
            .Where(x =>
                x.EntityType == "Case" &&
                x.EntityId == caseId &&
                (x.Action == AuditAction.CaseCreated ||
                    x.Action == AuditAction.NoteAdded ||
                    x.Action == AuditAction.Assigned ||
                    x.Action == AuditAction.StatusChanged ||
                    x.Action == AuditAction.ClosureRequested ||
                    x.Action == AuditAction.ApprovalApproved ||
                    x.Action == AuditAction.ApprovalRejected ||
                    x.Action == AuditAction.CaseReopened))
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new
            {
                x.Id,
                x.Action,
                Actor = x.ActorUser == null
                    ? null
                    : new CaseUserSummaryDto(x.ActorUser.Id, x.ActorUser.DisplayName),
                x.CreatedAtUtc,
                x.MetadataJson
            })
            .ToListAsync(cancellationToken);

        var assignmentUserIds = auditLogs
            .Where(x => x.Action == AuditAction.Assigned)
            .Select(x => TryReadAssignmentMetadata(x.MetadataJson))
            .SelectMany(x => new[] { x?.FromUserId, x?.ToUserId })
            .OfType<Guid>()
            .Distinct()
            .ToArray();

        var usersById = assignmentUserIds.Length == 0
            ? new Dictionary<Guid, string>()
            : await dbContext.Users
                .AsNoTracking()
                .Where(x => assignmentUserIds.Contains(x.Id))
                .ToDictionaryAsync(x => x.Id, x => x.DisplayName, cancellationToken);

        return auditLogs
            .Select(x => new CaseTimelineItemDto(
                x.Id,
                x.Action.ToString(),
                x.Actor,
                x.CreatedAtUtc,
                DescriptionFor(x.Action, x.MetadataJson, usersById)))
            .ToList();
    }

    private static string DescriptionFor(
        AuditAction action,
        string? metadataJson,
        IReadOnlyDictionary<Guid, string> usersById)
    {
        if (action == AuditAction.CaseCreated)
        {
            return "Case created";
        }

        if (action == AuditAction.NoteAdded)
        {
            return "Note added";
        }

        if (action == AuditAction.StatusChanged)
        {
            var statusMetadata = TryReadStatusMetadata(metadataJson);
            if (!string.IsNullOrWhiteSpace(statusMetadata?.FromStatus) &&
                !string.IsNullOrWhiteSpace(statusMetadata.ToStatus))
            {
                return $"Status changed from {statusMetadata.FromStatus} to {statusMetadata.ToStatus}";
            }

            return "Status changed";
        }

        if (action == AuditAction.CaseReopened)
        {
            return "Case reopened";
        }

        if (action == AuditAction.ClosureRequested)
        {
            return AppendReason("Closure approval requested", TryReadApprovalMetadata(metadataJson)?.RequestReason);
        }

        if (action == AuditAction.ApprovalApproved)
        {
            return AppendReason("Closure approved", TryReadApprovalMetadata(metadataJson)?.DecisionReason);
        }

        if (action == AuditAction.ApprovalRejected)
        {
            return AppendReason("Closure rejected", TryReadApprovalMetadata(metadataJson)?.DecisionReason);
        }

        var metadata = TryReadAssignmentMetadata(metadataJson);
        if (metadata?.ToUserId is not { } toUserId ||
            !usersById.TryGetValue(toUserId, out var toDisplayName))
        {
            return "Case assigned";
        }

        if (metadata.FromUserId is { } fromUserId &&
            usersById.TryGetValue(fromUserId, out var fromDisplayName))
        {
            return $"Reassigned from {fromDisplayName} to {toDisplayName}";
        }

        return $"Assigned to {toDisplayName}";
    }

    private static AssignmentMetadata? TryReadAssignmentMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<AssignmentMetadata>(metadataJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static StatusMetadata? TryReadStatusMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<StatusMetadata>(metadataJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static ApprovalMetadata? TryReadApprovalMetadata(string? metadataJson)
    {
        if (string.IsNullOrWhiteSpace(metadataJson))
        {
            return null;
        }

        try
        {
            return JsonSerializer.Deserialize<ApprovalMetadata>(metadataJson, JsonOptions);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    private static string AppendReason(string description, string? reason)
    {
        return string.IsNullOrWhiteSpace(reason) ? description : $"{description}: {reason}";
    }

    private sealed record AssignmentMetadata(Guid? FromUserId, Guid? ToUserId, string? Reason);

    private sealed record StatusMetadata(string? FromStatus, string? ToStatus, string? Reason);

    private sealed record ApprovalMetadata(string? RequestReason, string? DecisionReason);
}
