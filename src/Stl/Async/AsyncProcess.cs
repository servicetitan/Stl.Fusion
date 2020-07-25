using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;

namespace Stl.Async
{
    public interface IAsyncProcess : IAsyncDisposable, IHostedService
    {
        CancellationToken StopToken { get; }
        Task? RunningTask { get; }
        Task RunAsync();
    }

    public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
    {
        private object Lock => StopTokenSource;
        protected CancellationTokenSource StopTokenSource { get; }
        public CancellationToken StopToken { get; }
        public Task? RunningTask { get; private set; }

        protected AsyncProcessBase()
        {
            StopTokenSource = new CancellationTokenSource();
            StopToken = StopTokenSource.Token;
        }

        public Task RunAsync()
        {
            if (RunningTask != null)
                return RunningTask;
            lock (Lock) {
                if (RunningTask != null)
                    return RunningTask;
                ThrowIfDisposedOrDisposing();
                RunningTask = Task
                    .Run(() => RunInternalAsync(StopToken), CancellationToken.None)
                    .SuppressCancellation();
            }
            return RunningTask;
        }

        protected abstract Task RunInternalAsync(CancellationToken cancellationToken);

        public Task StartAsync(CancellationToken cancellationToken)
        {
            RunAsync();
            return Task.CompletedTask;
        }

        public async Task StopAsync(CancellationToken cancellationToken)
            => await DisposeAsync();

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            if (!StopTokenSource.IsCancellationRequested)
                StopTokenSource.Cancel();
            await (RunningTask ?? Task.CompletedTask).ConfigureAwait(false);
            StopTokenSource?.Dispose();
            await base.DisposeInternalAsync(disposing);
        }
    }
}
