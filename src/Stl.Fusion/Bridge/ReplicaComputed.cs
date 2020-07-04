namespace Stl.Fusion.Bridge
{
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

        public ReplicaComputed(ComputedOptions options, ReplicaInput input, LTag lTag) 
            : base(options, input, lTag) { }
        public ReplicaComputed(ComputedOptions options, ReplicaInput input, Result<T> output, LTag lTag, bool isConsistent = true) 
            : base(options, input, output, lTag, isConsistent) { }
    }
}
