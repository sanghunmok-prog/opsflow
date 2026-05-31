namespace OpsFlow.Application.Cases;

public sealed class CaseListQuery
{
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
    public string? Search { get; init; }
    public string? Status { get; init; }
    public string? Priority { get; init; }
    public Guid? CaseTypeId { get; init; }
    public Guid? AssignedToUserId { get; init; }
    public bool? Overdue { get; init; }
    public string? SortBy { get; init; } = "createdAtUtc";
    public string? SortDirection { get; init; } = "desc";
}
