namespace Stl.Fusion.Bridge;

public interface IReplicaComputed : IComputed
{
    IReplica Replica { get; }
}

public interface IReplicaComputed<T> : IComputed<ReplicaInput, T>, IReplicaComputed
{
    new IReplica<T> Replica { get; }
}

public class ReplicaComputed<T> : Computed<ReplicaInput, T>, IReplicaComputed<T>
{
    IReplica IReplicaComputed.Replica => Input.Replica;
    public IReplica<T> Replica => (IReplica<T>) Input.Replica;

    protected ReplicaComputed(ComputedOptions options, ReplicaInput input, LTag version)
        : base(options, input, version)
    { }

    public ReplicaComputed(ComputedOptions options, ReplicaInput input, Result<T> output, LTag version, bool isConsistent)
        : base(options, input, output, version, isConsistent)
    { }
}
