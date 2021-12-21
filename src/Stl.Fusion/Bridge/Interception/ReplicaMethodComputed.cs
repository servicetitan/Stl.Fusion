using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Interception;

public interface IReplicaMethodComputed : IComputed
{
    IReplica? Replica { get; }
}

public interface IReplicaMethodComputed<T> : IComputed<ComputeMethodInput, T>, IReplicaMethodComputed
{
    new IReplica<T>? Replica { get; }
}

public class ReplicaMethodComputed<T> : Computed<ComputeMethodInput, T>, IReplicaMethodComputed<T>
{
    IReplica? IReplicaMethodComputed.Replica => Replica;
    public IReplica<T>? Replica { get; }

    // Two primary constructors
    public ReplicaMethodComputed(ComputedOptions options, ComputeMethodInput input, IReplicaComputed<T> source)
        : this(source.Replica, options, input, source.Version)
    {
        ((IComputedImpl) this).AddUsed((IComputedImpl) source);
        // ReSharper disable once VirtualMemberCallInConstructor
        TrySetOutput(source.Output);
        if (!source.IsConsistent())
            Invalidate();
    }

    public ReplicaMethodComputed(ComputedOptions options, ComputeMethodInput input, Exception error, LTag version)
        : this(null, options, input, new Result<T>(default!, error), version, false) { }

    // And the "inherited" ones allowing to configure this computed as you wish
    protected ReplicaMethodComputed(IReplica<T>? replica,
        ComputedOptions options, ComputeMethodInput input, LTag version)
        : base(options, input, version)
        => Replica = replica;

    protected ReplicaMethodComputed(IReplica<T>? replica,
        ComputedOptions options, ComputeMethodInput input,
        Result<T> output, LTag version, bool isConsistent)
        : base(options, input, output, version, isConsistent)
        => Replica = replica;

    protected override void OnInvalidated()
    {
        // We intentionally suppress ComputedRegistry.Unregister here,
        // otherwise it won't be possible to find IReplica using
        // old IComputed.
        CancelTimeouts();
    }
}
