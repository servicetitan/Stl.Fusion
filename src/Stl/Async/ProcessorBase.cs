namespace Stl.Async;

public abstract class ProcessorBase : IAsyncDisposable, IDisposable, IHasWhenDisposed
{
    private volatile Task? _disposeTask;

    protected CancellationTokenSource StopTokenSource { get; }
    protected object Lock => StopTokenSource;

    public CancellationToken StopToken { get; }
    public Task? WhenDisposed => _disposeTask;

    protected ProcessorBase(CancellationTokenSource? stopTokenSource = null)
    {
        StopTokenSource = stopTokenSource ?? new CancellationTokenSource();
        StopToken = StopTokenSource.Token;
    }

    public void Dispose()
        => _ = DisposeAsync();

    public async ValueTask DisposeAsync()
    {
        Task disposeTask;
        lock (Lock) {
            if (_disposeTask == null) {
                StopTokenSource.CancelAndDisposeSilently();
                _disposeTask = DisposeAsyncCore();
            }
            disposeTask = _disposeTask;
        }
        await disposeTask.ConfigureAwait(false);
    }

    protected virtual Task DisposeAsyncCore()
        => Task.CompletedTask;
}
