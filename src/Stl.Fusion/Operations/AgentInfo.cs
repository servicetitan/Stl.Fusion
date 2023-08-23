using Stl.OS;

namespace Stl.Fusion.Operations;

public record AgentInfo(Symbol Id)
{
    private static long _nextId;
    private static long GetNextId() => Interlocked.Increment(ref _nextId);

    public AgentInfo()
        : this($"{RuntimeInfo.Process.MachinePrefixedId}-{GetNextId()}")
    { }
}
