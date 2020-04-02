using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Internal;

namespace Stl.Locking
{
    public class AsyncLockSet<TKey> : IAsyncLockSet<TKey>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<Unit>> _locks =
            new ConcurrentDictionary<TKey, TaskCompletionSource<Unit>>();
        private readonly AsyncLocal<Dictionary<TKey, int>>? _localLocks;

        public ReentryMode ReentryMode { get; }
        public int AcquiredLockCount => _locks.Count;

        public AsyncLockSet(ReentryMode reentryMode)
        {
            ReentryMode = reentryMode;
            _localLocks = ReentryMode != ReentryMode.UncheckedDeadlock
                ? new AsyncLocal<Dictionary<TKey, int>>()
                : null;
        }

        public bool IsLocked(TKey key) => _locks.ContainsKey(key);

        public bool? IsLockedLocally(TKey key)
            => _localLocks == null 
                ? (bool?) null 
                : _localLocks.Value?.ContainsKey(key) ?? false;

        public ValueTask<IDisposable> LockAsync(
            TKey key, CancellationToken cancellationToken = default)
        {
            // This has to be done in non-async method, otherwise the Value
            // that's set below won't "propagate" back to the calling async method.
            if (_localLocks != null)
                _localLocks.Value ??= new Dictionary<TKey, int>()!;
            return InternalLockAsync(key, cancellationToken);
        }

        private async ValueTask<IDisposable> InternalLockAsync(
            TKey key, CancellationToken cancellationToken = default)
        {
            var localLocks = _localLocks?.Value;
            var myLock = new TaskCompletionSource<Unit>();
            var cancellationTask = (Task?) null;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (_locks.TryAdd(key, myLock))
                    break;
                if (!_locks.TryGetValue(key, out var existingLock))
                    continue;
                if (localLocks?.ContainsKey(key) == true)
                    return CreateDisposable(key, myLock, localLocks);
                cancellationTask ??= cancellationToken.ToTask(true);
                await Task.WhenAny(existingLock.Task, cancellationTask).ConfigureAwait(false);
                // No need to spin here: the probability of seeing another
                // lock in TryAdd and not seeing it in TryGetValue is nearly
                // zero (i.e. it was removed right between these calls).
            }
            return CreateDisposable(key, myLock, localLocks!);
        }

        protected IDisposable CreateDisposable(TKey key, 
            TaskCompletionSource<Unit> myLock, Dictionary<TKey, int> localLocks)
        {
            if (localLocks != null) {
                var reentryCount = localLocks.GetValueOrDefault(key) + 1;
                if (reentryCount > 1 && ReentryMode == ReentryMode.CheckedFail)
                    throw Errors.AlreadyLocked();
                localLocks[key] = reentryCount;
            }

            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New(state => {
                var (self, key2, myLock2) = state;
                // Removing local lock
                var reentryCount = 0;
                var localLocks1 = self._localLocks?.Value;
                if (localLocks1 != null) {
                    reentryCount = localLocks1.GetValueOrDefault(key2) - 1;
                    localLocks1[key2] = reentryCount;
                }
                Debug.Assert(reentryCount >= 0);
                if (reentryCount == 0) {
                    myLock2.SetResult(default); // Must be done before TryRemove
                    self._locks.TryRemove(key2, myLock2);
                }
            }, (this, key, myLock));
        }
    }
}
