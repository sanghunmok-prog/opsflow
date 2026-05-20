using OpsFlow.Api.Data.Enums;

namespace OpsFlow.Api.Data.Entities;

public sealed class SlaRule
{
    public Guid Id { get; set; }
    public Guid CaseTypeId { get; set; }
    public CaseType CaseType { get; set; } = null!;
    public CasePriority Priority { get; set; }
    public int TargetHours { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
