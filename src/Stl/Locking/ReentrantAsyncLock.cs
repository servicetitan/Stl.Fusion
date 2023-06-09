using Stl.Internal;
using Stl.Locking.Internal;

namespace Stl.Locking;

public sealed class ReentrantAsyncLock : AsyncLock
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

    public override ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default)
    {
        var reentryCounter = ReentryCounter;
        if (reentryCounter == null)
            ReentryCounter = new(1);
        else {
            var oldReentryCount = reentryCounter.Value++;
            if (oldReentryCount > 0) {
                if (ReentryMode == LockReentryMode.CheckedFail) {
                    reentryCounter.Value = oldReentryCount;
                    throw Errors.AlreadyLocked();
                }
                return ValueTaskExt.FromResult(new AsyncLockReleaser(this));
            }
        }

        var task = _semaphore.WaitAsync(cancellationToken);
        return AsyncLockReleaser.NewWhenCompleted(task, this);
    }

    public override void Release()
    {
        var reentryCounter = ReentryCounter;
        if (reentryCounter == null)
            return;

        var newReentryCount = --reentryCounter.Value;
        if (newReentryCount <= 0) {
            // < 0 is something that should never happen,
            // but throwing an error here is probably worse, so...
            ReentryCounter = null;
            _semaphore.Release();
        }
    }
}
