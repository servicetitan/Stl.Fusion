using System.Diagnostics;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework;

public abstract class DbWakeSleepProcessBase<TDbContext> : DbAsyncProcessBase<TDbContext>
    where TDbContext : DbContext
{
    protected DbWakeSleepProcessBase(IServiceProvider services) : base(services) { }

    protected override async Task RunInternal(CancellationToken cancellationToken)
    {
        var activityName = $"{nameof(WakeUp)}:{GetType().ToSymbol()}";
        while (!cancellationToken.IsCancellationRequested) {
            var error = default(Exception?);
            try {
                using var activity = FusionTrace.StartActivity(activityName);
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
