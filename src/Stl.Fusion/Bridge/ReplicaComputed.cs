using Stl.Fusion.Bridge.Internal;

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

        public ReplicaComputed(ReplicaInput input, LTag lTag) : base(input, lTag) { }
        public ReplicaComputed(ReplicaInput input, Result<T> output, LTag lTag, bool isConsistent = true) 
            : base(input, output, lTag, isConsistent) { }
    }
}
