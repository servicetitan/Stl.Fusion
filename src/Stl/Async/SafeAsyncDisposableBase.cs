namespace Stl.Async;

/// <summary>
/// A safer version of
/// https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
/// that ensures <see cref="DisposeAsync(bool)"/> is called just once.
/// </summary>
public abstract class SafeAsyncDisposableBase : IAsyncDisposable, IDisposable, IHasWhenDisposed
{
    private volatile int _isDisposing;
    private volatile Task? _disposeTask;

    public Task? WhenDisposed => _disposeTask;

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposing, 1, 0) != 0)
            return;

        var disposeTask = DisposeAsync(true);
        _ = Interlocked.Exchange(ref _disposeTask, disposeTask);
        GC.SuppressFinalize(this);
    }

    public ValueTask DisposeAsync()
    {
        Task? disposeTask;
        if (Interlocked.CompareExchange(ref _isDisposing, 1, 0) != 0) {
            var spinWait = new SpinWait();
            while (true) {
                disposeTask = _disposeTask;
                if (disposeTask != null)
                    return disposeTask.ToValueTask();
                spinWait.SpinOnce(); // Safe for WASM
            }
        }

        disposeTask = DisposeAsync(true);
        _ = Interlocked.Exchange(ref _disposeTask, disposeTask);
        GC.SuppressFinalize(this);
        return disposeTask.ToValueTask();
    }

    protected abstract Task DisposeAsync(bool disposing);

    protected bool MarkDisposed()
    {
        if (Interlocked.CompareExchange(ref _isDisposing, 1, 0) != 0)
            return false;

        _ = Interlocked.Exchange(ref _disposeTask, Task.CompletedTask);
        GC.SuppressFinalize(this);
        return true;
    }
}
