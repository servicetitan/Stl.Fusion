using System;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Interception
{
    public interface IReplicaClientComputed : IComputed
    {
        IReplica? Replica { get; }
    }

    public interface IReplicaClientComputed<T> : IComputed<InterceptedInput, T>, IReplicaClientComputed
    {
        new IReplica<T>? Replica { get; }
    }

    public class ReplicaClientComputed<T> : Computed<InterceptedInput, T>, IReplicaClientComputed<T>
    {
        IReplica? IReplicaClientComputed.Replica => Replica;
        public IReplica<T>? Replica { get; }

        // Two primary constructors
        public ReplicaClientComputed(ComputedOptions options, IReplicaComputed<T> source, InterceptedInput input)
            : this(options, source.Replica, input, source.Version)
        {
            ((IComputedImpl) this).AddUsed((IComputedImpl) source);
            TrySetOutput(source.Output);
            if (!source.IsConsistent)
                Invalidate();
        }

        public ReplicaClientComputed(ComputedOptions options, InterceptedInput input, Exception error, LTag version)
            : this(options, null, input, new Result<T>(default!, error), version, false) { }

        // And the "inherited" ones allowing to configure this computed as you wish
        public ReplicaClientComputed(
            ComputedOptions options, IReplica<T>? replica, InterceptedInput input, LTag version)
            : base(options, input, version)
            => Replica = replica;
        public ReplicaClientComputed(
            ComputedOptions options, IReplica<T>? replica, InterceptedInput input,
            Result<T> output, LTag version, bool isConsistent = true)
            : base(options, input, output, version, isConsistent)
            => Replica = replica;
    }
}
