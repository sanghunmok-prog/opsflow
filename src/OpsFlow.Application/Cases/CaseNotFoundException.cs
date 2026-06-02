namespace OpsFlow.Application.Cases;

public sealed class CaseNotFoundException(Guid caseId) : Exception($"Case {caseId} was not found.");
