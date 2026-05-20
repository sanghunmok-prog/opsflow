namespace OpsFlow.Api.Data.Entities;

public sealed class CaseNote
{
    public Guid Id { get; set; }
    public Guid CaseId { get; set; }
    public OpsCase Case { get; set; } = null!;
    public Guid AuthorUserId { get; set; }
    public AppUser AuthorUser { get; set; } = null!;
    public required string Body { get; set; }
    public DateTime CreatedAtUtc { get; set; }
}
