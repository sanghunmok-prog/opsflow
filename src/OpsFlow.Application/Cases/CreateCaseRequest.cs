using System.ComponentModel.DataAnnotations;

namespace OpsFlow.Application.Cases;

public sealed class CreateCaseRequest
{
    [Required]
    [StringLength(200)]
    public string? Title { get; init; }

    [StringLength(4000)]
    public string? Description { get; init; }

    [Required]
    public Guid? CaseTypeId { get; init; }

    [Required]
    public string? Priority { get; init; }
}
