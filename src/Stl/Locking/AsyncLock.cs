using System;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;
using Stl.Locking.Internal;

namespace Stl.Locking 
{
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

        async ValueTask<IDisposable> IAsyncLock.LockAsync(CancellationToken cancellationToken) 
            => await LockAsync(cancellationToken).ConfigureAwait(false);
        public ValueTask<Disposable<(AsyncLock, TaskCompletionStruct<Unit>, ReentryCounter?)>> LockAsync(
            CancellationToken cancellationToken = default)
        {
            // This has to be done in non-async method, otherwise the Value
            // that's set below won't "propagate" back to the calling async method.
            if (_reentryCounter == null)
                return InternalLockAsync(null, cancellationToken);
            var reentryCounter = _reentryCounter.Value ??= new ReentryCounter();
            return InternalLockAsync(reentryCounter, cancellationToken);
        }

        protected async ValueTask<Disposable<(AsyncLock, TaskCompletionStruct<Unit>, ReentryCounter?)>> InternalLockAsync(
            ReentryCounter? reentryCounter, CancellationToken cancellationToken = default)
        {
            var newLockTcs = new TaskCompletionStruct<Unit>(_taskCreationOptions);
            var cancellationTask = (Task?) null;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (reentryCounter?.TryReenter(ReentryMode) == true)
                    return CreateUnlocker(default, reentryCounter);
                var oldLock = Interlocked.CompareExchange(ref _lock, newLockTcs.Task, null);
                if (oldLock == null)
                    return CreateUnlocker(newLockTcs, reentryCounter);
                if (oldLock.IsCompleted)
                    continue; // Task.WhenAny will return immediately, so let's save a bit 
                cancellationTask ??= cancellationToken.ToTask(true);
                await Task.WhenAny(oldLock, cancellationTask).ConfigureAwait(false);
            }
        }

        protected Disposable<(AsyncLock, TaskCompletionStruct<Unit>, ReentryCounter?)> CreateUnlocker(
            TaskCompletionStruct<Unit> lockTcs, ReentryCounter? reentryCounter)
        {
            if (lockTcs.IsValid)
                reentryCounter?.Enter(ReentryMode);

            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New((this, lockTcs, reentryCounter), state => {
                var (self, lockTcs1, reentryCounter1) = state;
                // We should leave reentry first. ReentryCounter.Count == 0 will prevent
                // locks from the same async flow to re-enter w/o actually acquiring
                // a lock, i.e. they'll be forced to acquire their own locks.
                reentryCounter1?.Leave(); 
                if (lockTcs1.IsEmpty)
                    return;

                var lock1 = lockTcs1.Task;
                var oldLock = Interlocked.CompareExchange(ref self._lock, null, lock1);
                if (oldLock != lock1)
                    throw Errors.InternalError("Something is off with AsyncLock!");

                // And this should be done at the very end
                lockTcs1.SetResult(default); 
            });
        }
    }
}
