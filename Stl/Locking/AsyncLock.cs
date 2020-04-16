using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;

namespace Stl.Locking 
{
    public class AsyncLock : IAsyncLock
    {
        private volatile TaskCompletionSource<Unit>? _lock;
        private readonly AsyncLocal<Box<int>>? _localLocks;
        
        public ReentryMode ReentryMode { get; }
        public bool IsLocked => _lock != null; 
        public bool? IsLockedLocally => _localLocks == null 
            ? (bool?) null 
            : _localLocks.Value?.Value > 0;

        public AsyncLock(ReentryMode reentryMode)
        {
            ReentryMode = reentryMode;
            _localLocks = ReentryMode != ReentryMode.UncheckedDeadlock 
                ? new AsyncLocal<Box<int>>() 
                : null;
        }

        public ValueTask<IDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            // This has to be done in non-async method, otherwise the Value
            // that's set below won't "propagate" back to the calling async method.
            if (_localLocks != null)
                _localLocks.Value ??= new Box<int>()!;
            return InternalLockAsync(cancellationToken);
        }

        public async ValueTask<IDisposable> InternalLockAsync(CancellationToken cancellationToken = default)
        {
            var localLocks = _localLocks?.Value;
            var myLock = new TaskCompletionSource<Unit>();
            var cancellationTask = (Task?) null;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var existingLock = Interlocked.CompareExchange(ref _lock, myLock, null);
                if (existingLock == null)
                    break;
                if (localLocks?.Value > 0)
                    return CreateDisposable(myLock, localLocks);
                if (existingLock.Task.IsCompleted)
                    // Task.WhenAny will return immediately, so let's save a bit
                    continue; 
                cancellationTask ??= cancellationToken.ToTask(true);
                await Task.WhenAny(existingLock.Task, cancellationTask).ConfigureAwait(false);
            }
            return CreateDisposable(myLock, localLocks!);
        }

        protected IDisposable CreateDisposable(
            TaskCompletionSource<Unit> myLock, Box<int> localLocks)
        {
            if (localLocks != null) {
                var reentryCount = ++localLocks.Value;
                if (reentryCount > 1 && ReentryMode == ReentryMode.CheckedFail)
                    throw Errors.AlreadyLocked();
            }

            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New((this, myLock), state => {
                var (self, myLock1) = state;
                var oldLock = Interlocked.CompareExchange(ref self._lock, null, myLock1);
                if (oldLock == myLock1) {
                    var reentryCount = 0;
                    var localLocks1 = self._localLocks?.Value;
                    if (localLocks1 != null) 
                        reentryCount = --localLocks1.Value;
                    Debug.Assert(reentryCount >= 0);
                    if (reentryCount == 0)
                        // Must be done after setting _lock to null
                        myLock1.SetResult(default); 
                }
            });
        }
    }
}
