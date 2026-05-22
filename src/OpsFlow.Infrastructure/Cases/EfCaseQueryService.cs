using Microsoft.EntityFrameworkCore;
using OpsFlow.Application.Auth;
using OpsFlow.Application.Cases;
using OpsFlow.Domain.Entities;
using OpsFlow.Domain.Enums;
using OpsFlow.Infrastructure.Data;
using CaseUserSummaryDto = OpsFlow.Application.Cases.UserSummaryDto;

namespace OpsFlow.Infrastructure.Cases;

public sealed class EfCaseQueryService(
    OpsFlowDbContext dbContext,
    ICurrentUserService currentUser) : ICaseQueryService
{
    private const int MaxPageSize = 100;

    public async Task<PagedResult<CaseListItemDto>> GetCasesAsync(
        CaseListQuery query,
        CancellationToken cancellationToken = default)
    {
        ValidatePaging(query.Page, query.PageSize);

        var sortBy = NormalizeSortBy(query.SortBy);
        var sortDirection = NormalizeSortDirection(query.SortDirection);
        var status = ParseEnum<CaseStatus>(query.Status, "status");
        var priority = ParseEnum<CasePriority>(query.Priority, "priority");

        var cases = ApplyAccessScope(dbContext.Cases.AsNoTracking(), query.AssignedToUserId);

        if (!string.IsNullOrWhiteSpace(query.Search))
        {
            var search = query.Search.Trim();
            cases = cases.Where(x =>
                x.CaseNumber.Contains(search) ||
                x.Title.Contains(search) ||
                x.Description.Contains(search));
        }

        if (status is not null)
        {
            cases = cases.Where(x => x.Status == status);
        }

        if (priority is not null)
        {
            cases = cases.Where(x => x.Priority == priority);
        }

        if (query.CaseTypeId is not null)
        {
            cases = cases.Where(x => x.CaseTypeId == query.CaseTypeId);
        }

        if (currentUser.IsManagerOrAdmin && query.AssignedToUserId is not null)
        {
            cases = cases.Where(x => x.AssignedToUserId == query.AssignedToUserId);
        }

        cases = ApplySort(cases, sortBy, sortDirection);

        var totalCount = await cases.CountAsync(cancellationToken);
        var items = await cases
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(x => new CaseListItemDto(
                x.Id,
                x.CaseNumber,
                x.Title,
                new CaseTypeSummaryDto(x.CaseType.Id, x.CaseType.Name),
                x.Priority.ToString(),
                x.Status.ToString(),
                x.AssignedToUser == null
                    ? null
                    : new CaseUserSummaryDto(x.AssignedToUser.Id, x.AssignedToUser.DisplayName),
                x.CreatedAtUtc,
                x.DueAtUtc))
            .ToListAsync(cancellationToken);

        return new PagedResult<CaseListItemDto>(
            items,
            query.Page,
            query.PageSize,
            totalCount,
            (int)Math.Ceiling(totalCount / (double)query.PageSize));
    }

    public async Task<CaseDetailDto?> GetCaseAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var opsCase = await dbContext.Cases
            .AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id,
                x.CaseNumber,
                x.Title,
                x.Description,
                CaseType = new CaseTypeSummaryDto(x.CaseType.Id, x.CaseType.Name),
                x.Priority,
                x.Status,
                x.AssignedToUserId,
                AssignedTo = x.AssignedToUser == null
                    ? null
                    : new CaseUserSummaryDto(x.AssignedToUser.Id, x.AssignedToUser.DisplayName),
                CreatedBy = new CaseUserSummaryDto(x.CreatedByUser.Id, x.CreatedByUser.DisplayName),
                x.CreatedAtUtc,
                x.UpdatedAtUtc,
                x.DueAtUtc,
                x.RowVersion
            })
            .SingleOrDefaultAsync(cancellationToken);

        if (opsCase is null)
        {
            return null;
        }

        if (!CanAccessCase(opsCase.AssignedToUserId))
        {
            throw new CaseAccessDeniedException("The current user cannot access this case.");
        }

        return new CaseDetailDto(
            opsCase.Id,
            opsCase.CaseNumber,
            opsCase.Title,
            opsCase.Description,
            opsCase.CaseType,
            opsCase.Priority.ToString(),
            opsCase.Status.ToString(),
            opsCase.AssignedTo,
            opsCase.CreatedBy,
            opsCase.CreatedAtUtc,
            opsCase.UpdatedAtUtc,
            opsCase.DueAtUtc,
            Convert.ToBase64String(opsCase.RowVersion));
    }

    private IQueryable<OpsCase> ApplyAccessScope(IQueryable<OpsCase> cases, Guid? requestedAssignedToUserId)
    {
        var userId = currentUser.UserId
            ?? throw new CaseAccessDeniedException("The current user is not authenticated.");

        if (currentUser.IsManagerOrAdmin)
        {
            return cases;
        }

        if (currentUser.IsAnalyst)
        {
            if (requestedAssignedToUserId is not null && requestedAssignedToUserId != userId)
            {
                throw new CaseAccessDeniedException("Analysts can only filter by their own assigned cases.");
            }

            return cases.Where(x => x.AssignedToUserId == userId);
        }

        throw new CaseAccessDeniedException("The current user cannot access cases.");
    }

    private bool CanAccessCase(Guid? assignedToUserId)
    {
        if (currentUser.UserId is not { } userId)
        {
            return false;
        }

        return currentUser.IsManagerOrAdmin ||
            (currentUser.IsAnalyst && assignedToUserId == userId);
    }

    private static void ValidatePaging(int page, int pageSize)
    {
        if (page < 1)
        {
            throw new CaseQueryValidationException("page must be greater than or equal to 1.");
        }

        if (pageSize is < 1 or > MaxPageSize)
        {
            throw new CaseQueryValidationException($"pageSize must be between 1 and {MaxPageSize}.");
        }
    }

    private static TEnum? ParseEnum<TEnum>(string? value, string parameterName)
        where TEnum : struct, Enum
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.All(char.IsDigit) ||
            !Enum.TryParse<TEnum>(normalized, ignoreCase: true, out var parsed) ||
            !Enum.IsDefined(parsed))
        {
            throw new CaseQueryValidationException($"Invalid {parameterName}.");
        }

        return parsed;
    }

    private static string NormalizeSortBy(string? sortBy)
    {
        var normalized = string.IsNullOrWhiteSpace(sortBy) ? "createdAtUtc" : sortBy.Trim();
        return normalized switch
        {
            "caseNumber" or "createdAtUtc" or "dueAtUtc" or "priority" or "status" => normalized,
            _ => throw new CaseQueryValidationException("Invalid sortBy.")
        };
    }

    private static string NormalizeSortDirection(string? sortDirection)
    {
        var normalized = string.IsNullOrWhiteSpace(sortDirection) ? "desc" : sortDirection.Trim().ToLowerInvariant();
        return normalized switch
        {
            "asc" or "desc" => normalized,
            _ => throw new CaseQueryValidationException("Invalid sortDirection.")
        };
    }

    private static IQueryable<OpsCase> ApplySort(IQueryable<OpsCase> cases, string sortBy, string sortDirection)
    {
        var descending = sortDirection == "desc";
        return sortBy switch
        {
            "caseNumber" => descending
                ? cases.OrderByDescending(x => x.CaseNumber)
                : cases.OrderBy(x => x.CaseNumber),
            "createdAtUtc" => descending
                ? cases.OrderByDescending(x => x.CreatedAtUtc).ThenByDescending(x => x.CaseNumber)
                : cases.OrderBy(x => x.CreatedAtUtc).ThenBy(x => x.CaseNumber),
            "dueAtUtc" => descending
                ? cases.OrderByDescending(x => x.DueAtUtc).ThenByDescending(x => x.CaseNumber)
                : cases.OrderBy(x => x.DueAtUtc).ThenBy(x => x.CaseNumber),
            "priority" => descending
                ? cases.OrderByDescending(x => x.Priority).ThenByDescending(x => x.CreatedAtUtc)
                : cases.OrderBy(x => x.Priority).ThenBy(x => x.CreatedAtUtc),
            "status" => descending
                ? cases.OrderByDescending(x => x.Status).ThenByDescending(x => x.CreatedAtUtc)
                : cases.OrderBy(x => x.Status).ThenBy(x => x.CreatedAtUtc),
            _ => throw new CaseQueryValidationException("Invalid sortBy.")
        };
    }
}
