using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public interface IAsyncProcess : IAsyncDisposable
    {
        CancellationToken StoppingToken { get; }
        Task? RunningTask { get; }
        Task RunAsync();
    }

    public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
    {
        protected CancellationTokenSource StoppingTokenSource { get; } = new CancellationTokenSource();
        
        public CancellationToken StoppingToken => StoppingTokenSource.Token;
        public Task? RunningTask { get; private set; }

        public Task RunAsync()
        {
            lock (StoppingTokenSource) {
                if (RunningTask == null)
                    // ReSharper disable once MethodSupportsCancellation
                    RunningTask = Task.Run(RunInternalAsync).SuppressCancellation();
            }
            return RunningTask;
        }

        protected abstract Task RunInternalAsync();

        protected override async ValueTask DisposeInternalAsync(bool disposing)
        {
            if (!StoppingTokenSource.IsCancellationRequested)
                StoppingTokenSource.Cancel();
            await (RunningTask ?? Task.CompletedTask).ConfigureAwait(false);
            StoppingTokenSource.Dispose();
            await base.DisposeInternalAsync(disposing);
        }
    }
}
