using Stl.Internal;
using Stl.Locking.Internal;

namespace Stl.Locking;

public enum ReentryMode
{
    CheckedFail = 0,
    CheckedPass,
    UncheckedDeadlock,
}

public interface IAsyncLock
{
    ReentryMode ReentryMode { get; }
    bool IsLocked { get; }
    bool? IsLockedLocally { get; }
    ValueTask<IDisposable> Lock(CancellationToken cancellationToken = default);
}

public class AsyncLock : IAsyncLock
{
    private volatile Task? _lock;
    private readonly AsyncLocal<ReentryCounter>? _reentryCounter;
    private readonly TaskCreationOptions _taskCreationOptions;

    public ReentryMode ReentryMode { get; }
    public bool IsLocked => _lock != null;
    public bool? IsLockedLocally => _reentryCounter == null
        ? (bool?) null // No reentry counter -> we don't track reentry
        : _reentryCounter.Value?.Count > 0;

    public AsyncLock(ReentryMode reentryMode,
        TaskCreationOptions taskCreationOptions = TaskCreationOptions.RunContinuationsAsynchronously)
    {
        ReentryMode = reentryMode;
        _taskCreationOptions = taskCreationOptions;
        _reentryCounter = ReentryMode != ReentryMode.UncheckedDeadlock
            ? new AsyncLocal<ReentryCounter>()
            : null;
    }

    ValueTask<IDisposable> IAsyncLock.Lock(CancellationToken cancellationToken)
    {
        // This has to be non-async method, otherwise AsyncLocals
        // created inside it won't be available in caller's ExecutionContext.
        if (_reentryCounter == null)
            return SlowInternalLock(null, cancellationToken);
        var reentryCounter = _reentryCounter.Value ??= new ReentryCounter();
        return SlowInternalLock(reentryCounter, cancellationToken);
    }

    public ValueTask<Releaser> Lock(
        CancellationToken cancellationToken = default)
    {
        // This has to be non-async method, otherwise AsyncLocals
        // created inside it won't be available in caller's ExecutionContext.
        if (_reentryCounter == null)
            return FastInternalLock(null, cancellationToken);
        var reentryCounter = _reentryCounter.Value ??= new ReentryCounter();
        return FastInternalLock(reentryCounter, cancellationToken);
    }

    protected async ValueTask<IDisposable> SlowInternalLock(
        ReentryCounter? reentryCounter, CancellationToken cancellationToken = default)
        // ReSharper disable once HeapView.BoxingAllocation
        => await FastInternalLock(reentryCounter, cancellationToken).ConfigureAwait(false);

    protected async ValueTask<Releaser> FastInternalLock(
        ReentryCounter? reentryCounter, CancellationToken cancellationToken = default)
    {
        var newLockSrc = TaskSource.New<Unit>(_taskCreationOptions);
        var dCancellationTokenTask = new Disposable<Task, CancellationTokenRegistration>();
        try {
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (reentryCounter?.TryReenter(ReentryMode) == true)
                    return new Releaser(this, default, reentryCounter);
                var oldLock = Interlocked.CompareExchange(ref _lock, newLockSrc.Task, null);
                if (oldLock == null)
                    return new Releaser(this, newLockSrc, reentryCounter);
                if (oldLock.IsCompleted)
                    continue; // Task.WhenAny will return immediately, so let's save a bit
                if (dCancellationTokenTask.Resource == null!)
                    dCancellationTokenTask = cancellationToken.ToTask();
                await Task.WhenAny(oldLock, dCancellationTokenTask.Resource).ConfigureAwait(false);
            }
        }
        finally {
            dCancellationTokenTask.Dispose();
        }
    }

    public readonly struct Releaser : IDisposable
    {
        private readonly AsyncLock _owner;
        private readonly TaskSource<Unit> _taskSource;
        private readonly ReentryCounter? _reentryCounter;

        public Releaser(AsyncLock owner, TaskSource<Unit> taskSource, ReentryCounter? reentryCounter)
        {
            _owner = owner;
            _taskSource = taskSource;
            _reentryCounter = reentryCounter;

            if (!taskSource.IsEmpty)
                reentryCounter?.Enter(owner.ReentryMode);
        }

        public void Dispose()
        {
            // We should leave reentry first. ReentryCounter.Count == 0 will prevent
            // locks from the same async flow to re-enter w/o actually acquiring
            // a lock, i.e. they'll be forced to acquire their own locks.
            _reentryCounter?.Leave();
            if (_taskSource.IsEmpty)
                return;

            var lock1 = _taskSource.Task;
            var oldLock = Interlocked.CompareExchange(ref _owner._lock, null, lock1);
            if (oldLock != lock1)
                throw Errors.InternalError("Something is off with AsyncLock!");

            // And this should be done at the very end
            _taskSource.SetResult(default);
        }
    }
}
