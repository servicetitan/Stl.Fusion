using Stl.Locking.Internal;

namespace Stl.Locking;

public abstract class AsyncLock
{
    public static AsyncLock New(LockReentryMode reentryMode)
        => reentryMode == LockReentryMode.Unchecked
            ? new SimpleAsyncLock()
            : new ReentrantAsyncLock(reentryMode);

    public abstract ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default);
    public abstract void Release();
}
