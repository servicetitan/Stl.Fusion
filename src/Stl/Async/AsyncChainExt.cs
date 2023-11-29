using System.Diagnostics;

namespace Stl.Async;

public static class AsyncChainExt
{
    // RunXxx

    public static Task Run(
        this IEnumerable<AsyncChain> chains,
        CancellationToken cancellationToken = default)
        => chains.Run(false, cancellationToken);

    public static Task RunIsolated(
        this IEnumerable<AsyncChain> chains,
        CancellationToken cancellationToken = default)
        => chains.Run(true, cancellationToken);

    public static Task Run(
        this IEnumerable<AsyncChain> chains,
        bool isolate,
        CancellationToken cancellationToken = default)
    {
        var tasks = new List<Task>();
        using (isolate ? ExecutionContextExt.TrySuppressFlow() : default)
            foreach (var chain in chains)
                tasks.Add(chain.Run(cancellationToken));
        return Task.WhenAll(tasks);
    }

    // Construction primitives

    public static AsyncChain Rename(this AsyncChain asyncChain, string name)
        => asyncChain with { Name = name };

    public static AsyncChain WithTerminalErrorDetector(this AsyncChain asyncChain, TerminalErrorDetector terminalErrorDetector)
        => asyncChain with { TerminalErrorDetector = terminalErrorDetector };

    public static AsyncChain Append(this AsyncChain asyncChain, AsyncChain suffixChain)
        => new($"{asyncChain.Name} & {suffixChain.Name}", async cancellationToken => {
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            await suffixChain.Start(cancellationToken).ConfigureAwait(false);
        }, asyncChain.TerminalErrorDetector);

    public static AsyncChain Prepend(this AsyncChain asyncChain, AsyncChain prefixChain)
        => new($"{prefixChain.Name} & {asyncChain.Name}", async cancellationToken => {
            await prefixChain.Start(cancellationToken).ConfigureAwait(false);
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
        }, asyncChain.TerminalErrorDetector);

    public static AsyncChain LogError(this AsyncChain asyncChain, ILogger? log)
    {
        if (log == null)
            return asyncChain;

        return asyncChain with {
            Start = async cancellationToken => {
                try {
                    await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) {
                    if (!asyncChain.TerminalErrorDetector.Invoke(e, cancellationToken))
                        log.LogError(e, "{ChainName} failed", asyncChain.Name);
                    throw;
                }
            }
        };
    }

    public static AsyncChain Log(this AsyncChain asyncChain, ILogger? log)
        => asyncChain.Log(LogLevel.Information, log);
    public static AsyncChain Log(this AsyncChain asyncChain, LogLevel logLevel, ILogger? log)
    {
        if (log == null)
            return asyncChain;

        return asyncChain with {
            Start = async cancellationToken => {
                log.IfEnabled(logLevel)?.Log(logLevel, "{ChainName} started", asyncChain.Name);
                try {
                    await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                    log.IfEnabled(logLevel)?.Log(logLevel,
                        "{ChainName} completed", asyncChain.Name);
                }
                catch (Exception e) {
                    if (!asyncChain.TerminalErrorDetector.Invoke(e, cancellationToken))
                        log.LogError(e, "{ChainName} failed", asyncChain.Name);
                    else
                        log.IfEnabled(logLevel)?.Log(logLevel,
                            "{ChainName} completed (terminal error)", asyncChain.Name);
                    throw;
                }
            }
        };
    }

    public static AsyncChain Trace(this AsyncChain asyncChain, Func<Activity?>? activityFactory, ILogger? log = null)
    {
        if (activityFactory == null)
            return asyncChain.LogError(log);
        return asyncChain with {
            Start = async cancellationToken => {
                using var activity = activityFactory.Invoke();
                await asyncChain.LogError(log).Start(cancellationToken).ConfigureAwait(false);
            }
        };
    }

    public static AsyncChain Silence(this AsyncChain asyncChain, ILogger? log = null)
        => new($"{asyncChain.Name}.Silence()", async cancellationToken => {
            try {
                await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            }
            catch (Exception e) {
                if (!asyncChain.TerminalErrorDetector.Invoke(e, cancellationToken))
                    log?.IfEnabled(LogLevel.Error)?.LogError(e,
                        "{ChainName} failed, the error is silenced", asyncChain.Name);
            }
        }, asyncChain.TerminalErrorDetector);

    public static AsyncChain AppendDelay(this AsyncChain asyncChain, Func<RandomTimeSpan> delayFactory, IMomentClock? clock = null)
        => asyncChain.AppendDelay(() => delayFactory.Invoke().Next());
    public static AsyncChain AppendDelay(this AsyncChain asyncChain, Func<TimeSpan> delayFactory, IMomentClock? clock = null)
    {
        clock ??= MomentClockSet.Default.CpuClock;
        return new($"{asyncChain.Name}.AppendDelay(?)", async cancellationToken => {
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            await clock.Delay(delayFactory.Invoke(), cancellationToken).ConfigureAwait(false);
        }, asyncChain.TerminalErrorDetector);
    }
    public static AsyncChain AppendDelay(this AsyncChain asyncChain, RandomTimeSpan delay, IMomentClock? clock = null)
    {
        clock ??= MomentClockSet.Default.CpuClock;
        return new($"{asyncChain.Name}.AppendDelay({delay})", async cancellationToken => {
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
            await clock.Delay(delay.Next(), cancellationToken).ConfigureAwait(false);
        }, asyncChain.TerminalErrorDetector);
    }

    public static AsyncChain PrependDelay(this AsyncChain asyncChain, Func<RandomTimeSpan> delayFactory, IMomentClock? clock = null)
        => asyncChain.PrependDelay(() => delayFactory.Invoke().Next());
    public static AsyncChain PrependDelay(this AsyncChain asyncChain, Func<TimeSpan> delayFactory, IMomentClock? clock = null)
    {
        clock ??= MomentClockSet.Default.CpuClock;
        return new($"{asyncChain.Name}.PrependDelay(?)", async cancellationToken => {
            await clock.Delay(delayFactory.Invoke(), cancellationToken).ConfigureAwait(false);
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
        }, asyncChain.TerminalErrorDetector);
    }
    public static AsyncChain PrependDelay(this AsyncChain asyncChain, RandomTimeSpan delay, IMomentClock? clock = null)
    {
        clock ??= MomentClockSet.Default.CpuClock;
        return new($"{asyncChain.Name}.PrependDelay({delay})", async cancellationToken => {
            await clock.Delay(delay.Next(), cancellationToken).ConfigureAwait(false);
            await asyncChain.Start(cancellationToken).ConfigureAwait(false);
        }, asyncChain.TerminalErrorDetector);
    }

    public static AsyncChain RetryForever(this AsyncChain asyncChain, RetryDelaySeq retryDelays, ILogger? log = null)
        => asyncChain.RetryForever(retryDelays, null, log);
    public static AsyncChain RetryForever(this AsyncChain asyncChain, RetryDelaySeq retryDelays, IMomentClock? clock, ILogger? log = null)
        => new($"{asyncChain.Name}.RetryForever({retryDelays}", async cancellationToken => {
            clock ??= MomentClockSet.Default.CpuClock;
            for (var failedTryCount = 0;; failedTryCount++) {
                try {
                    if (failedTryCount >= 1)
                        log?.IfEnabled(LogLevel.Information)?.LogInformation(
                            "Retrying {ChainName} (#{FailedTryCount})",
                            asyncChain.Name, failedTryCount);
                    await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                    return;
                }
                catch (Exception e) {
                    if (asyncChain.TerminalErrorDetector.Invoke(e, cancellationToken))
                        throw;
                    // Everything else must be retried
                }
                var retryDelay = retryDelays[failedTryCount];
                await clock.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
            }
        }, asyncChain.TerminalErrorDetector);

    public static AsyncChain Retry(this AsyncChain asyncChain, RetryDelaySeq retryDelays, int? maxRetryCount, ILogger? log = null)
        => asyncChain.Retry(retryDelays, maxRetryCount, null, log);
    public static AsyncChain Retry(this AsyncChain asyncChain, RetryDelaySeq retryDelays, int? maxRetryCount, IMomentClock? clock, ILogger? log = null)
    {
        if (maxRetryCount is not { } maxCount)
            return asyncChain.RetryForever(retryDelays, log);

        return new($"{asyncChain.Name}.Retry({retryDelays}, {maxRetryCount})",
            async cancellationToken => {
                clock ??= MomentClockSet.Default.CpuClock;
                for (var failedTryCount = 0; failedTryCount <= maxCount; failedTryCount++) {
                    try {
                        if (failedTryCount >= 1)
                            log?.IfEnabled(LogLevel.Information)?.LogInformation(
                                "Retrying {ChainName} (#{FailedTryCount}/{MaxRetryCount})",
                                asyncChain.Name, failedTryCount, maxCount);
                        await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                        return;
                    }
                    catch (Exception e) {
                        if (asyncChain.TerminalErrorDetector.Invoke(e, cancellationToken) || failedTryCount >= maxCount)
                            throw;
                        // Everything else must be retried
                    }
                    var retryDelay = retryDelays[failedTryCount];
                    await clock.Delay(retryDelay, cancellationToken).ConfigureAwait(false);
                }
            }, asyncChain.TerminalErrorDetector);
    }

    public static AsyncChain CycleForever(this AsyncChain asyncChain)
        => new($"{asyncChain.Name}.CycleForever()", async cancellationToken => {
            while (true) {
                await asyncChain.Start(cancellationToken).ConfigureAwait(false);
                cancellationToken.ThrowIfCancellationRequested();
            }
            // ReSharper disable once FunctionNeverReturns
        }, asyncChain.TerminalErrorDetector);
}
