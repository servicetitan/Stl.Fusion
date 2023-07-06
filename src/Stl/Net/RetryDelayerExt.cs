namespace Stl.Net;

public static class RetryDelayerExt
{
    public static RetryDelay GetDelay(this IRetryDelayer retryDelayer,
        int tryIndex, RetryDelayLogger logger, CancellationToken cancellationToken = default)
    {
        try {
            var delay = retryDelayer.GetDelay(tryIndex, cancellationToken);
            if (delay.IsLimitExceeded) {
                logger.LogLimitExceeded();
                return delay;
            }
            if (delay.Task.IsCompleted)
                return delay;

            var duration = delay.EndsAt - retryDelayer.Clock.Now;
            if (duration <= TimeSpan.Zero)
                return delay;

            logger.LogDelay(tryIndex, duration);
            return delay;
        }
        catch (Exception error) {
            logger.LogError(error);
            throw;
        }
    }
}
