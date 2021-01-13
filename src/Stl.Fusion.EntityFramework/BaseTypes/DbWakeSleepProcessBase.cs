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

        protected override async Task RunInternalAsync(CancellationToken cancellationToken)
        {
            for (;;) {
                var error = default(Exception?);
                try {
                    await WakeAsync(cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException) {
                    throw;
                }
                catch (Exception e) {
                    error = e;
                    Log.LogError(e, "Error.");
                }
                await SleepAsync(error, cancellationToken).ConfigureAwait(false);
            }
        }

        protected abstract Task WakeAsync(CancellationToken cancellationToken);
        protected abstract Task SleepAsync(Exception? error, CancellationToken cancellationToken);
    }
}
