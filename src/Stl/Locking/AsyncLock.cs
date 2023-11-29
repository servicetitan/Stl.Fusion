using Stl.Internal;

namespace Stl.Locking;

public sealed class AsyncLock(LockReentryMode reentryMode = LockReentryMode.Unchecked)
    : IAsyncLock<AsyncLock.Releaser>
{
    private readonly AsyncLocal<LockedTag?>? _isLockedLocally
        = reentryMode == LockReentryMode.Unchecked ? null : new AsyncLocal<LockedTag?>();
    private readonly SemaphoreSlim _semaphore = new(1, 1);

    public LockReentryMode ReentryMode { get; } = reentryMode;

    public bool IsLockedLocally {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get => _isLockedLocally?.Value != null;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        set {
            if (_isLockedLocally == null)
                return;

            _isLockedLocally.Value = value ? LockedTag.Instance : null;
        }
    }

    async ValueTask<IAsyncLockReleaser> IAsyncLock.Lock(CancellationToken cancellationToken)
        => await Lock(cancellationToken).ConfigureAwait(false);
    public ValueTask<Releaser> Lock(CancellationToken cancellationToken = default)
    {
        if (IsLockedLocally)
            return ReentryMode == LockReentryMode.CheckedFail
                ? throw Errors.AlreadyLocked()
                : default;

        var task = _semaphore.WaitAsync(cancellationToken);
        return Releaser.NewWhenCompleted(task, this);
    }

    // Nested types

    public sealed class LockedTag
    {
        public static readonly LockedTag Instance = new();

        private LockedTag() { }
    }

    public readonly struct Releaser(AsyncLock? asyncLock) : IAsyncLockReleaser
    {
        internal static ValueTask<Releaser> NewWhenCompleted(Task task, AsyncLock asyncLock)
        {
            return task.IsCompletedSuccessfully()
                ? ValueTaskExt.FromResult(new Releaser(asyncLock))
                : CompleteAsynchronously(task, asyncLock);

            static async ValueTask<Releaser> CompleteAsynchronously(Task task1, AsyncLock asyncLock1)
            {
                await task1.ConfigureAwait(false);
                return new Releaser(asyncLock1);
            }
        }

        public void MarkLockedLocally()
        {
            if (asyncLock == null)
                return;

            asyncLock.IsLockedLocally = true;
        }

        public void Dispose()
        {
            if (asyncLock == null)
                return;

            asyncLock.IsLockedLocally = false;
            asyncLock._semaphore.Release();
        }
    }
}
