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

public sealed class EfCaseNoteService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser,
    IClock clock,
    ICaseAccessService caseAccessService) : ICaseNoteService
{
    private const int MaxBodyLength = 2000;
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<IReadOnlyList<CaseNoteDto>?> GetNotesAsync(
        Guid caseId,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessCaseAsync(caseId, cancellationToken);

        return await dbContext.CaseNotes
            .AsNoTracking()
            .Where(x => x.CaseId == caseId)
            .OrderBy(x => x.CreatedAtUtc)
            .ThenBy(x => x.Id)
            .Select(x => new CaseNoteDto(
                x.Id,
                x.Body,
                new CaseUserSummaryDto(x.AuthorUser.Id, x.AuthorUser.DisplayName),
                x.CreatedAtUtc))
            .ToListAsync(cancellationToken);
    }

    public async Task<CaseNoteDto?> AddNoteAsync(
        Guid caseId,
        CreateCaseNoteRequest? request,
        CancellationToken cancellationToken = default)
    {
        await EnsureCanAccessCaseAsync(caseId, cancellationToken);

        var userId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user cannot add notes.");
        var body = NormalizeBody(request?.Body);
        var createdAtUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var note = new CaseNote
        {
            Id = Guid.NewGuid(),
            CaseId = caseId,
            AuthorUserId = userId,
            Body = body,
            CreatedAtUtc = createdAtUtc
        };

        dbContext.CaseNotes.Add(note);
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = userId,
            EntityType = "Case",
            EntityId = caseId,
            Action = AuditAction.NoteAdded,
            MetadataJson = JsonSerializer.Serialize(new { noteId = note.Id }, JsonOptions),
            CreatedAtUtc = createdAtUtc
        });

        await dbContext.SaveChangesAsync(cancellationToken);

        return await dbContext.CaseNotes
            .AsNoTracking()
            .Where(x => x.Id == note.Id)
            .Select(x => new CaseNoteDto(
                x.Id,
                x.Body,
                new CaseUserSummaryDto(x.AuthorUser.Id, x.AuthorUser.DisplayName),
                x.CreatedAtUtc))
            .SingleAsync(cancellationToken);
    }

    private async Task EnsureCanAccessCaseAsync(Guid caseId, CancellationToken cancellationToken)
    {
        var access = await caseAccessService.GetAccessStatusAsync(caseId, cancellationToken);
        if (access == CaseAccessStatus.NotFound)
        {
            throw new CaseNotFoundException(caseId);
        }

        if (access == CaseAccessStatus.Forbidden)
        {
            throw new CaseAccessDeniedException("The current user cannot access this case.");
        }
    }

    private static string NormalizeBody(string? body)
    {
        if (body is null)
        {
            throw new CaseNoteValidationException("Body is required.");
        }

        var normalized = body.Trim();
        if (normalized.Length == 0)
        {
            throw new CaseNoteValidationException("Body is required.");
        }

        if (normalized.Length > MaxBodyLength)
        {
            throw new CaseNoteValidationException($"Body must be {MaxBodyLength} characters or fewer.");
        }

        return normalized;
    }
}
