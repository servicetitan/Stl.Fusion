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
        Task Run();
    }

    public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
    {
        private object Lock => StopTokenSource;
        protected CancellationTokenSource StopTokenSource { get; }
        protected bool FlowExecutionContext { get; set; }
        public CancellationToken StopToken { get; }
        public Task? RunningTask { get; private set; }

        protected AsyncProcessBase()
        {
            StopTokenSource = new CancellationTokenSource();
            StopToken = StopTokenSource.Token;
        }

        public Task Run()
        {
            if (RunningTask != null)
                return RunningTask;
            lock (Lock) {
                if (RunningTask != null)
                    return RunningTask;
                ThrowIfDisposedOrDisposing();

                var flowSuppressor =
                    (FlowExecutionContext && !ExecutionContext.IsFlowSuppressed())
                    ? Disposable.NewClosed(ExecutionContext.SuppressFlow(), d => d.Dispose())
                    : Disposable.NewClosed<AsyncFlowControl>(default, _ => {});
                using (flowSuppressor)
                    RunningTask = Task
                        .Run(() => RunInternal(StopToken), CancellationToken.None)
                        .SuppressCancellation();
            }
            return RunningTask;
        }

        protected abstract Task RunInternal(CancellationToken cancellationToken);

        Task IHostedService.StartAsync(CancellationToken cancellationToken) => Start(cancellationToken);
        public Task Start(CancellationToken cancellationToken = default)
        {
            Run();
            return Task.CompletedTask;
        }

        Task IHostedService.StopAsync(CancellationToken cancellationToken) => Stop(cancellationToken);
        public async Task Stop(CancellationToken cancellationToken = default)
            => await DisposeAsync();

        protected override async ValueTask DisposeInternal(bool disposing)
        {
            if (!StopTokenSource.IsCancellationRequested)
                StopTokenSource.Cancel();
            await (RunningTask ?? Task.CompletedTask).ConfigureAwait(false);
            StopTokenSource.Dispose();
        }
    }
}
