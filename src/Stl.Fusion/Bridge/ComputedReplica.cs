using Stl.Fusion.Bridge.Internal;

namespace Stl.Fusion.Bridge
{
    public interface IComputedReplica : IComputed
    {
        IReplica Replica { get; }
    }

    public interface IComputedReplica<T> : IComputed<ReplicaInput, T>, IComputedReplica
    {
        new IReplica<T> Replica { get; }
    }

    public class ComputedReplica<T> : Computed<ReplicaInput, T>, IComputedReplica<T>
    {
        IReplica IComputedReplica.Replica => Input.Replica;
        public IReplica<T> Replica => (IReplica<T>) Input.Replica;

        public ComputedReplica(ReplicaInput input, LTag lTag) : base(input, lTag) { }
        public ComputedReplica(ReplicaInput input, Result<T> output, LTag lTag, bool isConsistent = true) 
            : base(input, output, lTag, isConsistent) { }
    }
}
