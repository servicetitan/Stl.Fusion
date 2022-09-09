namespace Stl.Fusion.Bridge;

public interface IReplicaComputed : IComputed
{
    Replica Replica { get; }
}

public class ReplicaComputed<T> : Computed<T>, IReplicaComputed
{
    Replica IReplicaComputed.Replica => Replica;
    public Replica<T> Replica { get; }

    protected ReplicaComputed(ComputedOptions options, Replica<T> replica, LTag version)
        : base(options, replica, version)
        => Replica = replica;

    public ReplicaComputed(ComputedOptions options, Replica<T> replica, Result<T> output, LTag version, bool isConsistent)
        : base(options, replica, output, version, isConsistent) 
        => Replica = replica;
}
