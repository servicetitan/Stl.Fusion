using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Stl.Internal;

namespace Stl.Async;

public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
{
    private volatile int _isDisposed = 0;
    private readonly CancellationTokenSource _stopTokenSource;
    private readonly object _lock;

    protected bool MustFlowExecutionContext { get; init; } = false;

    public bool IsDisposeStarted => _isDisposed != 0;
    public CancellationToken StopToken { get; }
    public Task? RunningTask { get; private set; }
    public Task? DisposeTask => IsDisposeStarted ? RunningTask ?? Task.CompletedTask : null;

    protected AsyncProcessBase()
    {
        _stopTokenSource = new CancellationTokenSource();
        _lock = _stopTokenSource;
        StopToken = _stopTokenSource.Token;
    }

    protected override async ValueTask DisposeAsyncCore()
    {
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            await Stop().ConfigureAwait(false);
        else
            await (RunningTask ?? Task.CompletedTask).ConfigureAwait(false);
    }

    protected override void Dispose(bool disposing)
    {
        if (!disposing) return;

        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            Stop();
        _stopTokenSource.Dispose();
    }

    public async Task Run(CancellationToken cancellationToken)
    {
        await using var _ = cancellationToken.Register(StopInternal).ToAsyncDisposableAdapter();
        await Run().ConfigureAwait(false);
    }

    // Returns a task that always succeeds
    public Task Run()
    {
        if (RunningTask != null)
            return RunningTask;
        lock (_lock) {
            if (RunningTask != null)
                return RunningTask;
            if (StopToken.IsCancellationRequested)
                throw Errors.AlreadyDisposedOrDisposing();

            var flowSuppressor =
                (MustFlowExecutionContext && !ExecutionContext.IsFlowSuppressed())
                    ? Disposable.NewClosed(ExecutionContext.SuppressFlow(), d => d.Dispose())
                    : Disposable.NewClosed<AsyncFlowControl>(default, _ => {});
            using (flowSuppressor)
                RunningTask = Task
                    .Run(() => RunInternal(StopToken), CancellationToken.None)
                    .SuppressExceptions(); // !!! Important
        }
        return RunningTask;
    }

    protected abstract Task RunInternal(CancellationToken cancellationToken);

    public void Start()
        // ReSharper disable once MethodSupportsCancellation
        => Run();

    public Task Stop()
    {
        StopInternal();
        return RunningTask ?? Task.CompletedTask;
    }

    // IHostedService implementation

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        Start();
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => Stop();

    // Private methods

    private void StopInternal()
    {
        try {
            _stopTokenSource.Cancel();
        }
        catch {
            // Intended
        }
    }
}
