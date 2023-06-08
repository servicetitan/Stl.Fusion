using Stl.Locking.Internal;

namespace Stl.Locking;

public interface IAsyncLock
{
    ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default);
    void Release();
}

public sealed class AsyncLock : IAsyncLock
{
    private readonly SemaphoreSlim _semaphore;

    public static IAsyncLock New(LockReentryMode reentryMode)
        => reentryMode == LockReentryMode.Unchecked
            ? new AsyncLock()
            : new ReentrantAsyncLock(reentryMode);

    public AsyncLock()
        => _semaphore = new SemaphoreSlim(1, 1);

    public ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default)
    {
        var task = _semaphore.WaitAsync(cancellationToken);
        return AsyncLockReleaser.NewWhenCompleted(task, this);
    }

    public void Release()
        => _semaphore.Release();
}
