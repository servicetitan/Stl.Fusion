namespace Stl.Fusion.Bridge
{
    public interface IComputedReplicaRetryPolicy : IComputeRetryPolicy { }

    public class ComputedReplicaRetryPolicy : ComputeRetryPolicy, IComputedReplicaRetryPolicy
    {
        public new static readonly IComputedReplicaRetryPolicy Default = 
            new ComputedReplicaRetryPolicy(3);

        public ComputedReplicaRetryPolicy(int maxAttemptCount) : base(maxAttemptCount) { }
    }
}
