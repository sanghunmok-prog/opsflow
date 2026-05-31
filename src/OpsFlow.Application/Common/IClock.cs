namespace OpsFlow.Application.Common;

public interface IClock
{
    DateTime UtcNow { get; }
}
