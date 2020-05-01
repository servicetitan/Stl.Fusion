namespace Stl.Fusion
{
    public interface IComputeRetryPolicy
    {
        bool MustRetry(IComputed computed, int tryIndex);
    }

    public class ComputeRetryPolicy : IComputeRetryPolicy
    {
        public static readonly IComputeRetryPolicy Default = 
            new ComputeRetryPolicy(3);

        public int MaxTryCount { get; }

        public ComputeRetryPolicy(int maxAttemptCount) 
            => MaxTryCount = maxAttemptCount;

        public bool MustRetry(IComputed computed, int tryIndex) 
            => tryIndex < MaxTryCount;
    }
}
