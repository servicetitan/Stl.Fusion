using Stl.Internal;

namespace Stl.Locking;

public sealed class AsyncLock(LockReentryMode reentryMode)
    : IAsyncLock<AsyncLock.Releaser>
{
    private readonly AsyncLocal<LockedTag?> _isLockedLocally = new();
    private volatile TaskCompletionSource<Unit>? _whenReleasedSource;

    public LockReentryMode ReentryMode { get; } = reentryMode;

    public bool IsLockedLocally {
        get => _isLockedLocally.Value != null;
        set => _isLockedLocally.Value = value ? LockedTag.Instance : null;
    }

    async ValueTask<IAsyncLockReleaser> IAsyncLock.Lock(CancellationToken cancellationToken)
    {
        var releaser = await Lock(cancellationToken).ConfigureAwait(false);
        return releaser;
    }

    public ValueTask<Releaser> Lock(CancellationToken cancellationToken = default)
    {
        if (IsLockedLocally)
            return ReentryMode == LockReentryMode.CheckedFail
                ? throw Errors.AlreadyLocked()
                : default;

        Task<Unit>? whenReleased;
        lock (_isLockedLocally) {
            whenReleased = _whenReleasedSource?.Task;
            if (whenReleased == null || whenReleased.IsCompleted) {
                _whenReleasedSource = new();
                return new ValueTask<Releaser>(new Releaser(this));
            }
        }
        return LockAsync(whenReleased, cancellationToken).ToValueTask();
    }

    // Private methods

    private async Task<Releaser> LockAsync(Task<Unit>? whenReleased, CancellationToken cancellationToken)
    {
        if (whenReleased != null)
            await whenReleased.WaitAsync(cancellationToken).ConfigureAwait(false);
        while (true) {
            lock (_isLockedLocally) {
                whenReleased = _whenReleasedSource?.Task;
                if (whenReleased == null || whenReleased.IsCompleted) {
                    _whenReleasedSource = new();
                    return new Releaser(this);
                }
            }
            await whenReleased.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
    }

    // Nested types

    public sealed class LockedTag
    {
        public static readonly LockedTag Instance = new();

        private LockedTag() { }
    }

    public readonly struct Releaser(AsyncLock? asyncLock) : IAsyncLockReleaser
    {
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

            asyncLock._isLockedLocally.Value = null;
            // ReSharper disable once InconsistentlySynchronizedField
            asyncLock._whenReleasedSource?.TrySetResult(default);
        }
    }
}
