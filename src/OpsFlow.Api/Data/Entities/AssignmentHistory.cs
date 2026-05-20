namespace OpsFlow.Api.Data.Entities;

public sealed class AssignmentHistory
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public OpsCase Case { get; set; } = null!;
    public Guid? FromUserId { get; set; }
    public AppUser? FromUser { get; set; }
    public Guid ToUserId { get; set; }
    public AppUser ToUser { get; set; } = null!;
    public Guid AssignedByUserId { get; set; }
    public AppUser AssignedByUser { get; set; } = null!;
    public DateTime CreatedAtUtc { get; set; }
}
