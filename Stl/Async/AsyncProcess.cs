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
            RunningTask = RunInternalAsync().SuppressCancellation();
            return RunningTask;
        }

        protected abstract Task RunInternalAsync();

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            if (!StoppingTokenSource.IsCancellationRequested)
                StoppingTokenSource.Cancel();
            StoppingTokenSource.Dispose();
            return base.DisposeInternalAsync(disposing);
        }
    }
}
