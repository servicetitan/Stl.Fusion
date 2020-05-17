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

        public ComputedReplica(ReplicaInput input, int tag) : base(input, tag) { }
        public ComputedReplica(ReplicaInput input, Result<T> output, int tag, bool isConsistent = true) : base(input, output, tag, isConsistent) { }
    }
}
