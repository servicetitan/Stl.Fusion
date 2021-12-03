using Microsoft.Extensions.Hosting;
using Stl.Internal;

namespace Stl.Async;

public abstract class AsyncProcessBase : AsyncDisposableBase, IAsyncProcess
{
    private readonly CancellationTokenSource _stopTokenSource = new();
    private volatile int _isDisposed;

    protected object Lock => _stopTokenSource;
    protected bool MustFlowExecutionContext { get; init; } = false;

    public Task? RunningTask { get; private set; }
    public CancellationToken StopToken { get; }
    public bool IsDisposeStarted => _isDisposed != 0;

    protected AsyncProcessBase()
        => StopToken = _stopTokenSource.Token;

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

        var registration = cancellationToken.Register(
            static self => (self as AsyncProcessBase)?.StopInternal(),
            this);
        var result = Run();
        result.ContinueWith(_ => registration.Dispose(), TaskScheduler.Default);
        return result;
    }

    // Returns a task that always succeeds
    public Task Run()
    {
        if (RunningTask != null)
            return RunningTask;
        lock (Lock) {
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
                    .ContinueWith(_ => _stopTokenSource.Dispose(), TaskScheduler.Default); // !!! Important
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
