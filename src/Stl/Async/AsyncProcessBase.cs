using Microsoft.Extensions.Hosting;
using Stl.Internal;

namespace Stl.Async;

public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
{
    private volatile int _isDisposed = 0;
    private readonly CancellationTokenSource _stopTokenSource;
    private readonly object _lock;

    protected bool MustFlowExecutionContext { get; init; } = false;
    protected CancellationToken StopToken { get; }

    public bool IsDisposeStarted => _isDisposed != 0;
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
        // We disregard disposing here, since if this method is called
        // from finalizer, we still want to stop everything "gracefully",
        // i.e. by cancelling the StopToken.
        if (Interlocked.CompareExchange(ref _isDisposed, 1, 0) == 0)
            Stop();
    }

    public Task Run(CancellationToken cancellationToken)
    {
        if (cancellationToken == default)
            return Run();

        var registration = cancellationToken.Register(StopInternal);
        var result = Run();
        result.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        return result;
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
                throw Errors.AlreadyStopped();

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
        => _stopTokenSource.CancelAndDisposeSilently();
}
