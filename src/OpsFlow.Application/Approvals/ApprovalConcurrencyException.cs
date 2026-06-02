namespace OpsFlow.Application.Approvals;

public sealed class ApprovalConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
