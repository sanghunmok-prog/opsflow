using OpsFlow.Api.Data.Enums;

namespace OpsFlow.Api.Data.Entities;

public sealed class StatusHistory
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public OpsCase Case { get; set; } = null!;
    public CaseStatus? FromStatus { get; set; }
    public CaseStatus ToStatus { get; set; }
    public Guid ChangedByUserId { get; set; }
    public AppUser ChangedByUser { get; set; } = null!;
    public string? Reason { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
