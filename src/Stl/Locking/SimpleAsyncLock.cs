using Stl.Locking.Internal;

namespace Stl.Locking;

public sealed class SimpleAsyncLock : AsyncLock
{
    private readonly SemaphoreSlim _semaphore;

    public SimpleAsyncLock()
        => _semaphore = new SemaphoreSlim(1, 1);

    public override ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default)
    {
        var task = _semaphore.WaitAsync(cancellationToken);
        return AsyncLockReleaser.NewWhenCompleted(task, this);
    }

    public override void Release()
        => _semaphore.Release();
}
