using OpsFlow.Application.Common;

namespace OpsFlow.Infrastructure;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
