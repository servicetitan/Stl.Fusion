using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public interface IAsyncProcess : IAsyncDisposable
    {
        Task? RunningTask { get; }
        Task RunAsync();
    }

    public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
    {
        private readonly CancellationTokenSource _stopTokenSource = new CancellationTokenSource();
        protected CancellationToken StopToken => _stopTokenSource.Token;
        
        public Task? RunningTask { get; private set; }

        public Task RunAsync()
        {
            RunningTask = RunInternalAsync().SuppressCancellation();
            return RunningTask;
        }

        protected abstract Task RunInternalAsync();

        protected override ValueTask DisposeInternalAsync(bool disposing)
        {
            _stopTokenSource.Cancel();
            return base.DisposeInternalAsync(disposing);
        }
    }
}
