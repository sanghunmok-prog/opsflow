using OpsFlow.Api.Data.Enums;

namespace OpsFlow.Api.Data.Entities;

public sealed class AppUser
{
    public Guid Id { get; set; }
    public required string Email { get; set; }
    public required string DisplayName { get; set; }
    public UserRole Role { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<OpsCase> AssignedCases { get; } = new List<OpsCase>();
    public ICollection<OpsCase> CreatedCases { get; } = new List<OpsCase>();
    public ICollection<CaseNote> AuthoredNotes { get; } = new List<CaseNote>();
    public ICollection<StatusHistory> StatusChanges { get; } = new List<StatusHistory>();
    public ICollection<AssignmentHistory> AssignmentsReceived { get; } = new List<AssignmentHistory>();
    public ICollection<AssignmentHistory> AssignmentsMade { get; } = new List<AssignmentHistory>();
    public ICollection<ApprovalRequest> ApprovalRequests { get; } = new List<ApprovalRequest>();
    public ICollection<ApprovalRequest> ApprovalReviews { get; } = new List<ApprovalRequest>();
    public ICollection<AuditLog> AuditLogs { get; } = new List<AuditLog>();
}
