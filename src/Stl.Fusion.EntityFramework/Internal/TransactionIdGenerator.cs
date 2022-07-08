using System.Globalization;
using Stl.Generators;

namespace Stl.Fusion.EntityFramework.Internal;

public class TransactionIdGenerator : Generator<string>
{
    private long _nextId;
    protected string Prefix { get; init; }

    public TransactionIdGenerator(AgentInfo agentInfo)
        => Prefix = agentInfo.Id.Value;

    public override string Next()
        => $"{Prefix}-{NextId().ToString(CultureInfo.InvariantCulture)}";

    // Protected methods

    protected long NextId()
        => Interlocked.Increment(ref _nextId);
}

public class TransactionIdGenerator<TContext> : TransactionIdGenerator
{
    public TransactionIdGenerator(AgentInfo agentInfo) : base(agentInfo)
    {
        var contextName = typeof(TContext).Name;
        Prefix = $"{Prefix}-{contextName}";
    }
}
