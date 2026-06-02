namespace OpsFlow.Application.Approvals;

public sealed class ApprovalValidationException(string message) : Exception(message);
