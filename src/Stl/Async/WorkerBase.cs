using Microsoft.Extensions.Hosting;
using Stl.Internal;

namespace Stl.Async;

public abstract class WorkerBase : ProcessorBase, IWorker
{
    private volatile Task? _whenRunning;

    protected bool MustFlowExecutionContext { get; init; } = false;

    public Task? WhenRunning => _whenRunning;
    
    protected WorkerBase(CancellationToken cancellationToken) : base(cancellationToken) { }
    protected WorkerBase(CancellationTokenSource? stopTokenSource = null) : base(stopTokenSource) { }

    protected override Task DisposeAsyncCore() 
        => WhenRunning ?? Task.CompletedTask;

    // Returns a task that always succeeds
    public Task Run()
    {
        if (_whenRunning != null)
            return _whenRunning;
        lock (Lock) {
            if (_whenRunning != null)
                return _whenRunning;
            this.ThrowIfDisposedOrDisposing();
            if (StopToken.IsCancellationRequested)
                throw Errors.AlreadyStopped();

            var flowSuppressor =
                (MustFlowExecutionContext && !ExecutionContext.IsFlowSuppressed())
                    ? Disposable.NewClosed(ExecutionContext.SuppressFlow(), d => d.Dispose())
                    : Disposable.NewClosed<AsyncFlowControl>(default, _ => {});
            using (flowSuppressor)
                _whenRunning = Task
                    .Run(() => RunInternal(StopToken), CancellationToken.None)
                    .ContinueWith(_ => Stop(), TaskScheduler.Default);
        }
        return _whenRunning;
    }

    protected abstract Task RunInternal(CancellationToken cancellationToken);

    public void Start()
        => Run();

    public Task Stop()
    {
        StopTokenSource.CancelAndDisposeSilently();
        return WhenRunning ?? Task.CompletedTask;
    }

    // IHostedService implementation

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        Start();
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => Stop();
}
