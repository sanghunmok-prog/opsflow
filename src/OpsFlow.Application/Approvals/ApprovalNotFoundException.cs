namespace OpsFlow.Application.Approvals;

public sealed class ApprovalNotFoundException(Guid approvalId)
    : Exception($"Approval request '{approvalId}' was not found.");
