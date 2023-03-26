using Stl.Fusion.Interception;

namespace Stl.Fusion.Bridge.Interception;

public interface IReplicaMethodComputed : IComputed
{
    Replica? Replica { get; }
    PublicationStateInfo State { get; }
    bool HasReplica { get; }
}

public class ReplicaMethodComputed<T> : ComputeMethodComputed<T>, IReplicaMethodComputed
{
    Replica? IReplicaMethodComputed.Replica => Replica;
    PublicationStateInfo IReplicaMethodComputed.State => State;

    public Replica<T>? Replica { get; }
    public PublicationStateInfo<T> State { get; }
    public bool HasReplica => Replica is { IsDisposed: false };

    public ReplicaMethodComputed(ComputedOptions options, ComputeMethodInput input, Replica<T>? replica, PublicationStateInfo<T> state)
        : base(options, input, state.Output!.Value, state.Version, state.IsConsistent)
    {
        Replica = replica;
        State = state;
    }

    protected override void OnInvalidated()
    {
        // PseudoUnregister is used here just to trigger the
        // Unregistered event in ComputedRegistry.
        // We want to keep this computed while it's possible:
        // ReplicaMethodFunction.Compute tries to use it
        // to find a Replica to update through.
        // If this computed instance is gone from registry,  
        // a new Replica is going to be created for each call
        // to replica method.
        ComputedRegistry.Instance.PseudoUnregister(this);
        CancelTimeouts();
    }
}
