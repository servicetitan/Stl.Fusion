using System;
using Stl.Fusion.Interception;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Interception
{
    public interface IReplicaServiceComputed : IComputed
    {
        IReplica? Replica { get; }
    }

    public interface IReplicaServiceComputed<T> : IComputed<InterceptedInput, T>, IReplicaServiceComputed
    {
        new IReplica<T>? Replica { get; }
    }

    public class ReplicaServiceComputed<T> : Computed<InterceptedInput, T>, IReplicaServiceComputed<T>
    {
        IReplica? IReplicaServiceComputed.Replica => Replica;
        public IReplica<T>? Replica { get; }

        // Two primary constructors
        public ReplicaServiceComputed(ComputedOptions options, IReplicaComputed<T> source, InterceptedInput input) 
            : this(options, source.Replica, input, source.LTag)
        {
            ((IComputedImpl) this).AddUsed((IComputedImpl) source);
            TrySetOutput(source.Output);
            if (!source.IsConsistent)
                Invalidate();
        }
        
        public ReplicaServiceComputed(ComputedOptions options, InterceptedInput input, Exception error, LTag lTag) 
            : this(options, null, input, new Result<T>(default!, error), lTag, false) { } 

        // And the "inherited" ones allowing to configure this computed as you wish
        public ReplicaServiceComputed(
            ComputedOptions options, IReplica<T>? replica, InterceptedInput input, LTag lTag) 
            : base(options, input, lTag) 
            => Replica = replica;
        public ReplicaServiceComputed(
            ComputedOptions options, IReplica<T>? replica, InterceptedInput input, 
            Result<T> output, LTag lTag, bool isConsistent = true) 
            : base(options, input, output, lTag, isConsistent) 
            => Replica = replica;
    }
}
