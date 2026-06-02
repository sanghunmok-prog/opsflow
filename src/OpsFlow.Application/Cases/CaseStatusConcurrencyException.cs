namespace OpsFlow.Application.Cases;

public sealed class CaseStatusConcurrencyException(string message, Exception? innerException = null)
    : Exception(message, innerException);
