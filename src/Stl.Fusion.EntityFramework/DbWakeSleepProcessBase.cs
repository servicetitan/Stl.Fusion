using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Stl.Fusion.EntityFramework
{
    public abstract class DbWakeSleepProcessBase<TDbContext> : DbAsyncProcessBase<TDbContext>
        where TDbContext : DbContext
    {
        protected DbWakeSleepProcessBase(IServiceProvider services) : base(services) { }

        protected override async Task RunInternal(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested) {
                var error = default(Exception?);
                try {
                    await WakeUp(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    if (e is ObjectDisposedException ode && ode.Message.Contains("'IServiceProvider'"))
                        // Special case: this exception can be thrown on IoC container disposal,
                        // and if we don't handle it in a special way, DbWakeSleepProcessBase
                        // descendants may flood the log with exceptions till the moment they're stopped.
                        throw;
                    error = e;
                    Log.LogError(e, "WakeAsync error");
                }
                await Sleep(error, cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task WakeUp(CancellationToken cancellationToken);
        protected abstract Task Sleep(Exception? error, CancellationToken cancellationToken);
    }
}
