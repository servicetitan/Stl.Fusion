using System;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Interception
{
    public interface IReplicaClientComputed : IComputed
    {
        IReplica? Replica { get; }
    }

    public interface IReplicaClientComputed<T> : IComputed<ComputeMethodInput, T>, IReplicaClientComputed
    {
        new IReplica<T>? Replica { get; }
    }

    public class ReplicaClientComputed<T> : Computed<ComputeMethodInput, T>, IReplicaClientComputed<T>
    {
        IReplica? IReplicaClientComputed.Replica => Replica;
        public IReplica<T>? Replica { get; }

        // Two primary constructors
        public ReplicaClientComputed(ComputedOptions options, ComputeMethodInput input, IReplicaComputed<T> source)
            : this(source.Replica, options, input, source.Version)
        {
            ((IComputedImpl) this).AddUsed((IComputedImpl) source);
            // ReSharper disable once VirtualMemberCallInConstructor
            TrySetOutput(source.Output);
            if (!source.IsConsistent())
                Invalidate();
        }

        public ReplicaClientComputed(ComputedOptions options, ComputeMethodInput input, Exception error, LTag version)
            : this(null, options, input, new Result<T>(default!, error), version, false) { }

        // And the "inherited" ones allowing to configure this computed as you wish
        protected ReplicaClientComputed(IReplica<T>? replica,
            ComputedOptions options, ComputeMethodInput input, LTag version)
            : base(options, input, version)
            => Replica = replica;

        protected ReplicaClientComputed(IReplica<T>? replica,
            ComputedOptions options, ComputeMethodInput input,
            Result<T> output, LTag version, bool isConsistent)
            : base(options, input, output, version, isConsistent)
            => Replica = replica;

        protected override void OnInvalidated()
        {
            // We intentionally suppress ComputedRegistry.Unregister here,
            // otherwise it won't be possible to find IReplica using
            // old IComputed.
            this.CancelTimeouts();
        }
    }
}
