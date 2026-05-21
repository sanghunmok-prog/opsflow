using OpsFlow.Domain.Enums;

namespace OpsFlow.Domain.Entities;

public sealed class OpsCase
{
    public Guid Id { get; set; }
    public required string CaseNumber { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public Guid CaseTypeId { get; set; }
    public CaseType CaseType { get; set; } = null!;
    public CasePriority Priority { get; set; }
    public CaseStatus Status { get; set; }
    public Guid? AssignedToUserId { get; set; }
    public AppUser? AssignedToUser { get; set; }
    public Guid CreatedByUserId { get; set; }
    public AppUser CreatedByUser { get; set; } = null!;
    public DateTime DueAtUtc { get; set; }
    public DateTime? ResolvedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
    public byte[] RowVersion { get; set; } = [];

    public ICollection<CaseNote> Notes { get; } = new List<CaseNote>();
    public ICollection<StatusHistory> StatusHistories { get; } = new List<StatusHistory>();
    public ICollection<AssignmentHistory> AssignmentHistories { get; } = new List<AssignmentHistory>();
    public ICollection<ApprovalRequest> ApprovalRequests { get; } = new List<ApprovalRequest>();
}
