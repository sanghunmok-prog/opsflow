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

public sealed class EfCaseCommandService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser,
    IClock clock,
    ISlaService slaService) : ICaseCommandService
{
    private const string CaseNumberPrefix = "OPF";
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<CaseDetailDto> CreateCaseAsync(
        CreateCaseRequest request,
        CancellationToken cancellationToken = default)
    {
        var userId = currentUser.UserId
            ?? throw new CaseCommandValidationException("The current user is not authenticated.");
        var title = NormalizeRequired(request.Title, "Title");
        var description = request.Description?.Trim() ?? string.Empty;

        if (description.Length > 4000)
        {
            throw new CaseCommandValidationException("Description must be 4000 characters or fewer.");
        }

        if (request.CaseTypeId is not { } caseTypeId || caseTypeId == Guid.Empty)
        {
            throw new CaseCommandValidationException("CaseTypeId is required.");
        }

        if (!TryParseEnum<CasePriority>(request.Priority, out var priority))
        {
            throw new CaseCommandValidationException("Priority is invalid.");
        }

        var createdAtUtc = DateTime.SpecifyKind(clock.UtcNow, DateTimeKind.Utc);
        var dueAtUtc = await slaService.CalculateDueAtUtcAsync(
            caseTypeId,
            priority,
            createdAtUtc,
            cancellationToken);

        var opsCase = new OpsCase
        {
            Id = Guid.NewGuid(),
            CaseNumber = await GenerateCaseNumberAsync(createdAtUtc.Year, cancellationToken),
            Title = title,
            Description = description,
            CaseTypeId = caseTypeId,
            Priority = priority,
            Status = CaseStatus.New,
            AssignedToUserId = null,
            CreatedByUserId = userId,
            DueAtUtc = dueAtUtc,
            CreatedAtUtc = createdAtUtc,
            UpdatedAtUtc = createdAtUtc
        };

        dbContext.Cases.Add(opsCase);
        dbContext.AuditLogs.Add(new AuditLog
        {
            Id = Guid.NewGuid(),
            ActorUserId = userId,
            EntityType = "Case",
            EntityId = opsCase.Id,
            Action = AuditAction.CaseCreated,
            NewValuesJson = JsonSerializer.Serialize(new
            {
                opsCase.CaseNumber,
                Priority = opsCase.Priority.ToString(),
                Status = opsCase.Status.ToString(),
                opsCase.DueAtUtc
            }, JsonOptions),
            CreatedAtUtc = createdAtUtc
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
                x.Status != CaseStatus.Closed && createdAtUtc > x.DueAtUtc,
                Convert.ToBase64String(x.RowVersion)))
            .SingleAsync(cancellationToken);
    }

    private async Task<string> GenerateCaseNumberAsync(int year, CancellationToken cancellationToken)
    {
        var prefix = $"{CaseNumberPrefix}-{year}-";
        var caseNumbers = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.CaseNumber.StartsWith(prefix))
            .Select(x => x.CaseNumber)
            .ToListAsync(cancellationToken);

        var maxSuffix = caseNumbers
            .Select(x => int.TryParse(x[prefix.Length..], out var suffix) ? suffix : 0)
            .DefaultIfEmpty(0)
            .Max();

        return $"{prefix}{maxSuffix + 1:0000}";
    }

    private static string NormalizeRequired(string? value, string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new CaseCommandValidationException($"{fieldName} is required.");
        }

        var normalized = value.Trim();
        if (normalized.Length > 200)
        {
            throw new CaseCommandValidationException($"{fieldName} must be 200 characters or fewer.");
        }

        return normalized;
    }

    private static bool TryParseEnum<TEnum>(string? value, out TEnum parsed)
        where TEnum : struct, Enum
    {
        parsed = default;
        if (string.IsNullOrWhiteSpace(value) ||
            value.Trim().All(char.IsDigit) ||
            !Enum.TryParse(value.Trim(), ignoreCase: true, out parsed) ||
            !Enum.IsDefined(parsed))
        {
            return false;
        }

        return true;
    }
}
