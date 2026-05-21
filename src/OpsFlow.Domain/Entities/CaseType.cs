namespace OpsFlow.Domain.Entities;

public sealed class CaseType
{
    public Guid Id { get; set; }
    public required string Name { get; set; }
    public required string Description { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<SlaRule> SlaRules { get; } = new List<SlaRule>();
    public ICollection<OpsCase> Cases { get; } = new List<OpsCase>();
}
