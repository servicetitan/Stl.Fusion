using Stl.Internal;
using Stl.Locking.Internal;

namespace Stl.Locking;

public sealed class ReentrantAsyncLock : IAsyncLock
{
    private readonly SemaphoreSlim _semaphore = new(1, 1);
    private readonly AsyncLocal<AsyncLockReentryCounter?> _reentryCounter = new();

    private AsyncLockReentryCounter? ReentryCounter {
        get => _reentryCounter.Value;
        set => _reentryCounter.Value = value;
    }

    public LockReentryMode ReentryMode { get; }
    public bool IsLocked => ReentryCounter != null;

    public ReentrantAsyncLock(LockReentryMode reentryMode)
    {
        if (reentryMode == LockReentryMode.Unchecked)
            throw new ArgumentOutOfRangeException(nameof(reentryMode));

        ReentryMode = reentryMode;
    }

    public ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default)
    {
        var reentryCounter = ReentryCounter;
        if (reentryCounter != null) {
            if (ReentryMode == LockReentryMode.CheckedFail)
                throw Errors.AlreadyLocked();

            reentryCounter.Enter();
            return ValueTaskExt.FromResult(new AsyncLockReleaser(this));
        }

        ReentryCounter = new(1);
        var task = _semaphore.WaitAsync(cancellationToken);
        return AsyncLockReleaser.NewWhenCompleted(task, this);
    }

    public void Release()
    {
        var reentryCounter = ReentryCounter;
        if (reentryCounter == null)
            return;

        if (reentryCounter.Leave()) {
            ReentryCounter = null;
            _semaphore.Release();
        }
    }
}
