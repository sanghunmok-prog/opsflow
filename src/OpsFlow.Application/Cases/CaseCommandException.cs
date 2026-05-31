namespace OpsFlow.Application.Cases;

public abstract class CaseCommandException(string message) : Exception(message);

public sealed class CaseCommandValidationException(string message) : CaseCommandException(message);

public sealed class CaseTypeNotFoundException(string message) : CaseCommandException(message);

public sealed class SlaRuleNotFoundException(string message) : CaseCommandException(message);
