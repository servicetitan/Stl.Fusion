using System.Diagnostics;

namespace Stl.Async;

public static class AsyncChainExt
{
    public static AsyncChain Rename(this AsyncChain asyncChain, string name)
        => asyncChain with { Name = name };

    public static AsyncChain Append(this AsyncChain asyncChain, AsyncChain suffixChain)
        => new($"{asyncChain.Name} & {suffixChain.Name}", async cancellationToken => {
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            await suffixChain.Start(cancellationToken).ConfigureAwait(false);
        });

    public static AsyncChain Prepend(this AsyncChain asyncChain, AsyncChain prefixChain)
        => new($"{prefixChain.Name} & {asyncChain.Name}", async cancellationToken => {
            await prefixChain.Start(cancellationToken).ConfigureAwait(false);
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
        });

    public static AsyncChain Log(this AsyncChain asyncChain, ILogger? log)
    {
        if (log == null)
            return asyncChain;
        return asyncChain with {
            Start = async cancellationToken => {
                try {
                    await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) {
                    if (IsAlwaysThrowable(e)) throw;

                    log.LogError(e, "{ChainName} failed", asyncChain.Name);
                    throw;
                }
            }
        };
    }

    public static AsyncChain Trace(this AsyncChain asyncChain, Func<Activity?>? activityFactory, ILogger? log = null)
    {
        if (activityFactory == null)
            return asyncChain.Log(log);
        return asyncChain with {
            Start = async cancellationToken => {
                using var activity = activityFactory.Invoke();
                await asyncChain.Log(log).Start(cancellationToken).ConfigureAwait(false);
            }
        };
    }

    public static AsyncChain Silence(this AsyncChain asyncChain)
        => new($"{asyncChain.Name}.Silence()", async cancellationToken => {
            try {
                await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                if (IsAlwaysThrowable(e)) throw;
            }
        });

    public static AsyncChain RetryForever(this AsyncChain asyncChain, RetryDelaySeq retryDelays, ILogger? log = null)
        => asyncChain.RetryForever(retryDelays, null, log);
    public static AsyncChain RetryForever(this AsyncChain asyncChain, RetryDelaySeq retryDelays, IMomentClock? clock, ILogger? log = null)
        => new($"{asyncChain.Name}.RetryForever({retryDelays}", async cancellationToken => {
            clock ??= MomentClockSet.Default.CpuClock;
            for (var failedTryCount = 0;; failedTryCount++) {
                try {
                    if (failedTryCount >= 1)
                        log?.LogInformation("Retrying {ChainName} (#{FailedTryCount})", asyncChain.Name, failedTryCount);
                    await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception e) {
                    if (IsAlwaysThrowable(e)) throw;
                }
                var retryDelay = retryDelays[failedTryCount];
                await clock.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
        });

    public static AsyncChain Retry(this AsyncChain asyncChain, RetryDelaySeq retryDelays, int? maxRetryCount, ILogger? log = null)
        => asyncChain.Retry(retryDelays, maxRetryCount, null, log);
    public static AsyncChain Retry(this AsyncChain asyncChain, RetryDelaySeq retryDelays, int? maxRetryCount, IMomentClock? clock, ILogger? log = null)
    {
        if (maxRetryCount is not { } maxCount)
            return asyncChain.RetryForever(retryDelays, log);
        return new($"{asyncChain.Name}.Retry({retryDelays}, {maxRetryCount.ToString()})",
            async cancellationToken => {
                clock ??= MomentClockSet.Default.CpuClock;
                for (var failedTryCount = 0; failedTryCount <= maxCount; failedTryCount++) {
                    try {
                        if (failedTryCount >= 1)
                            log?.LogInformation("Retrying {ChainName} (#{FailedTryCount}/{MaxRetryCount})",
                                asyncChain.Name, failedTryCount, maxCount);
                        await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception e) {
                        if (IsAlwaysThrowable(e) || failedTryCount >= maxCount) throw;
                    }
                    var retryDelay = retryDelays[failedTryCount];
                    await clock.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                }
            });
    }

    public static AsyncChain Cycle(this AsyncChain asyncChain)
        => new($"{asyncChain.Name}.Cycle()", async cancellationToken => {
            while (true) {
                await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            }
            // ReSharper disable once FunctionNeverReturns
        });

    // Private methods

    private static bool IsAlwaysThrowable(Exception e)
    {
        if (e is OperationCanceledException)
            return true;
        if (e is ObjectDisposedException ode
#if NETSTANDARD2_0
            && ode.Message.Contains("'IServiceProvider'"))
#else
            && ode.Message.Contains("'IServiceProvider'", StringComparison.Ordinal))
#endif
            // Special case: this exception can be thrown on IoC container disposal,
            // and if we don't handle it in a special way, DbWakeSleepProcessBase
            // descendants may flood the log with exceptions till the moment they're stopped.
            return true;
        return false;
    }
}
