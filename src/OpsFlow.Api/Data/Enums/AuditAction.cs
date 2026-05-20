namespace OpsFlow.Api.Data.Enums;

public enum AuditAction
{
    CaseCreated,
    CaseUpdated,
    NoteAdded,
    StatusChanged,
    Assigned,
    ApprovalRequested,
    ApprovalApproved,
    ApprovalRejected,
    SlaDueDateCalculated,
    PriorityChanged
}
