using System.Diagnostics;

namespace Stl.Async;

public abstract class WakeSleepWorkerBase : WorkerBase
{
    private ActivitySource? _activitySource;

    protected ILogger Log { get; init; }
    protected ActivitySource ActivitySource {
        get => _activitySource ??= GetType().GetActivitySource();
        init => _activitySource = value;
    }

    protected WakeSleepWorkerBase(ILogger? log = null) 
        => Log = log ?? NullLogger.Instance;

    protected WakeSleepWorkerBase(CancellationTokenSource? stopTokenSource, ILogger? log = null)
        : base(stopTokenSource) 
        => Log = log ?? NullLogger.Instance;

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var operationName = GetType().GetOperationName(nameof(WakeUp));
        while (!cancellationToken.IsCancellationRequested) {
            var error = default(Exception?);
            try {
                using var activity = ActivitySource.StartActivity(operationName);
                await WakeUp(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                if (e is ObjectDisposedException ode
#if NETSTANDARD2_0
                && ode.Message.Contains("'IServiceProvider'"))
#else
                    && ode.Message.Contains("'IServiceProvider'", StringComparison.Ordinal))
#endif
                    // Special case: this exception can be thrown on IoC container disposal,
                    // and if we don't handle it in a special way, DbWakeSleepProcessBase
                    // descendants may flood the log with exceptions till the moment they're stopped.
                    throw;
                error = e;
                Log.LogError(e, "WakeUp failed");
            }
            await Sleep(error, cancellationToken).ConfigureAwait(false);
        }
    }

    protected abstract Task WakeUp(CancellationToken cancellationToken);
    protected abstract Task Sleep(Exception? error, CancellationToken cancellationToken);
}
