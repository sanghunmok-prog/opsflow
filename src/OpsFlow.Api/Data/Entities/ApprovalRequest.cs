using OpsFlow.Api.Data.Enums;

namespace OpsFlow.Api.Data.Entities;

public sealed class ApprovalRequest
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public OpsCase Case { get; set; } = null!;
    public Guid RequestedByUserId { get; set; }
    public AppUser RequestedByUser { get; set; } = null!;
    public Guid? ReviewedByUserId { get; set; }
    public AppUser? ReviewedByUser { get; set; }
    public ApprovalStatus Status { get; set; }
    public required string RequestReason { get; set; }
    public string? DecisionReason { get; set; }
    public DateTime RequestedAtUtc { get; set; }
    public DateTime? DecisionAtUtc { get; set; }
}
