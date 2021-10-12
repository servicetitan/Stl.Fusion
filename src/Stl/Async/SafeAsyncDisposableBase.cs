using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async;

/// <summary>
/// A safer version of
/// https://docs.microsoft.com/en-us/dotnet/standard/garbage-collection/implementing-disposeasync
/// that ensures <see cref="DisposeAsync(bool)"/> is called just once.
/// </summary>
public abstract class SafeAsyncDisposableBase : IAsyncDisposable, IDisposable, IHasDisposeStarted
{
    private volatile int _isDisposing;
    private volatile Task? _disposeTask;

    public bool IsDisposeStarted => _isDisposing != 0;

    public void Dispose()
    {
        if (Interlocked.CompareExchange(ref _isDisposing, 1, 0) != 0) return;

        Interlocked.Exchange(ref _disposeTask, DisposeAsync(true));
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.CompareExchange(ref _isDisposing, 1, 0) != 0) {
            var spinWait = new SpinWait();
            while (true) {
                var disposeTask1 = _disposeTask;
                if (disposeTask1 != null) {
                    await disposeTask1.ConfigureAwait(false);
                    return;
                }
                spinWait.SpinOnce();
            }
        }

        var disposeTask = DisposeAsync(true);
        _ = Interlocked.Exchange(ref _disposeTask, disposeTask);
        await disposeTask.ConfigureAwait(false);
#pragma warning disable CA1816 // Dispose methods should call SuppressFinalize
        GC.SuppressFinalize(this);
#pragma warning restore CA1816 // Dispose methods should call SuppressFinalize
    }

    protected abstract Task DisposeAsync(bool disposing);

    protected bool MarkDisposed()
    {
        if (Interlocked.CompareExchange(ref _isDisposing, 1, 0) != 0) return false;

        Interlocked.Exchange(ref _disposeTask, Task.CompletedTask);
        GC.SuppressFinalize(this);
        return true;
    }
}
