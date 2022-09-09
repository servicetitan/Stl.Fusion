using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Interception;

public interface IReplicaMethodComputed : IComputed
{
    Replica? Replica { get; }
}

public class ReplicaMethodComputed<T> : ComputeMethodComputed<T>, IReplicaMethodComputed
{
    Replica? IReplicaMethodComputed.Replica => Replica;
    public Replica<T>? Replica { get; }

    // Two primary constructors
    public ReplicaMethodComputed(ComputedOptions options, ComputeMethodInput input, ReplicaComputed<T> source)
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
    protected ReplicaMethodComputed(Replica<T>? replica,
        ComputedOptions options, ComputeMethodInput input, LTag version)
        : base(options, input, version)
        => Replica = replica;

    protected ReplicaMethodComputed(Replica<T>? replica,
        ComputedOptions options, ComputeMethodInput input,
        Result<T> output, LTag version, bool isConsistent)
        : base(options, input, output, version, isConsistent)
        => Replica = replica;

    protected override void OnInvalidated()
    {
        // We intentionally suppress ComputedRegistry.Unregister here,
        // otherwise it won't be possible to find Replica using
        // old IComputed.
        CancelTimeouts();
    }
}
