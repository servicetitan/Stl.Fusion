namespace Stl.Fusion
{
    public interface IRetryComputePolicy
    {
        bool MustRetry(IComputed computed, int tryIndex);
    }

    public class RetryComputePolicy : IRetryComputePolicy
    {
        public static readonly IRetryComputePolicy Default = 
            new RetryComputePolicy(3);

        public int MaxTryCount { get; }

        public RetryComputePolicy(int maxAttemptCount) 
            => MaxTryCount = maxAttemptCount;

        public bool MustRetry(IComputed computed, int tryIndex) 
            => tryIndex < MaxTryCount;
    }
}
