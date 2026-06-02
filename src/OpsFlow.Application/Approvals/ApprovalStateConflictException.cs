namespace OpsFlow.Application.Approvals;

public sealed class ApprovalStateConflictException(string message) : Exception(message);
