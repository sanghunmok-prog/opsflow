namespace OpsFlow.Application.Cases;

public sealed class CaseAccessDeniedException(string message) : Exception(message);
