using Stl.Fusion.Interception;

namespace Stl.Fusion.Bridge.Interception;

public interface IReplicaMethodComputed : IComputed
{
    Replica? Replica { get; }
    PublicationStateInfo? State { get; }
}

public class ReplicaMethodComputed<T> : ComputeMethodComputed<T>, IReplicaMethodComputed
{
    Replica? IReplicaMethodComputed.Replica => Replica;
    PublicationStateInfo? IReplicaMethodComputed.State => State;

    public Replica<T>? Replica { get; }
    public PublicationStateInfo<T>? State { get; }

    public ReplicaMethodComputed(ComputedOptions options, ComputeMethodInput input, Replica<T>? replica, PublicationStateInfo<T> state)
        : base(options, input, state.Output, state.Version, state.IsConsistent)
    {
        Replica = replica;
        State = state;
    }

    public ReplicaMethodComputed(ComputedOptions options, ComputeMethodInput input, Exception error, LTag version)
        : base(options, input, new Result<T>(default!, error), version, false)
    {
        Replica = null;
        State = null;
    }

    protected override void OnInvalidated()
    {
        // We intentionally suppress ComputedRegistry.Unregister here,
        // otherwise it won't be possible to find Replica using
        // the old IComputed.
        CancelTimeouts();
    }
}
