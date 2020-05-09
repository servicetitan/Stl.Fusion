using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public interface IAsyncProcess : IAsyncDisposable
    {
        CancellationToken StopToken { get; }
        Task? RunningTask { get; }
        Task RunAsync();
    }

    public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
    {
        protected CancellationTokenSource StopTokenSource { get; }
        protected object Lock => StopTokenSource;
        public CancellationToken StopToken { get; }
        public Task? RunningTask { get; private set; }

        protected AsyncProcessBase()
        {
            StopTokenSource = new CancellationTokenSource();
            StopToken = StopTokenSource.Token;
        }

        public Task RunAsync()
        {
            lock (Lock) {
                if (RunningTask == null)
                    // ReSharper disable once MethodSupportsCancellation
                    RunningTask = Task
                        .Run(() => RunInternalAsync(StopToken))
                        .SuppressCancellation();
            }
            return RunningTask;
        }

        protected abstract Task RunInternalAsync(CancellationToken cancellationToken);

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
