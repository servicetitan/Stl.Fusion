using Microsoft.Extensions.Hosting;
using Stl.Internal;

namespace Stl.Async;

public abstract class WorkerBase(CancellationTokenSource? stopTokenSource = null)
    : ProcessorBase(stopTokenSource), IWorker
{
    private volatile Task? _whenRunning;

    protected bool MustFlowExecutionContext { get; init; } = false;

    public Task? WhenRunning => _whenRunning;

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

            using var _ = MustFlowExecutionContext ? default : ExecutionContextExt.TrySuppressFlow();
            Task onStartTask;
            try {
                onStartTask = OnStart(StopToken);
            }
            catch (OperationCanceledException oce) {
                onStartTask = Task.FromCanceled(oce.CancellationToken);
            }
            catch (Exception e) {
                onStartTask = Task.FromException(e);
            }
            _whenRunning = Task.Run(async () => {
                try {
                    try {
                        await onStartTask.ConfigureAwait(false);
                        await OnRun(StopToken).ConfigureAwait(false);
                    }
                    finally {
                        StopTokenSource.CancelAndDisposeSilently();
                        await OnStop().ConfigureAwait(false);
                    }
                }
                catch {
                    // Intended: WhenRunning is returned by DisposeAsyncCore, so it should never throw
                }
            }, default);
        }
        return _whenRunning;
    }

    protected abstract Task OnRun(CancellationToken cancellationToken);
    protected virtual Task OnStart(CancellationToken cancellationToken) => Task.CompletedTask;
    protected virtual Task OnStop() => Task.CompletedTask;

    public Task Stop()
    {
        StopTokenSource.CancelAndDisposeSilently();
        return WhenRunning ?? Task.CompletedTask;
    }

    // IHostedService implementation

    Task IHostedService.StartAsync(CancellationToken cancellationToken)
    {
        _ = Run();
        return Task.CompletedTask;
    }

    Task IHostedService.StopAsync(CancellationToken cancellationToken)
        => Stop();
}
