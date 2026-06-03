namespace OpsFlow.Application.Cases;

public sealed class CaseAssignmentConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
