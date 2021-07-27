using System;

namespace Stl.Resilience
{
    public interface IRetryPolicy<in TTarget>
    {
        bool MustRetry(TTarget target, Exception error, int tryIndex);
    }

    public class RetryPolicy<TTarget> : IRetryPolicy<TTarget>
    {
        public static readonly IRetryPolicy<TTarget> Default = new RetryPolicy<TTarget>(3);

        public int MaxTryCount { get; }

        public RetryPolicy(int maxTryCount)
            => MaxTryCount = maxTryCount;

        public virtual bool MustRetry(TTarget target, Exception error, int tryIndex)
        {
            if (error is OperationCanceledException)
                return false;
            return tryIndex < MaxTryCount;
        }
    }
}
