using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        private readonly AsyncLocal<int>? _localLock;
        
        public ReentryMode ReentryMode { get; }

        public AsyncLock(ReentryMode reentryMode)
        {
            ReentryMode = reentryMode;
            _localLock = ReentryMode != ReentryMode.UncheckedDeadlock 
                ? new AsyncLocal<int>() 
                : null;
        }

        public ValueTask<bool> IsLockedAsync() => new ValueTask<bool>(_lock != null); 
        
        public bool? IsLockedLocally() => _localLock == null ? (bool?) null : _localLock.Value > 0;

        public void Prepare()
        {
        }

        public async ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            var myLock = new TaskCompletionSource<Unit>();
            var cancellationTask = (Task?) null;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var existingLock = Interlocked.CompareExchange(ref _lock, myLock, null);
                if (existingLock == null)
                    break;
                if (IsLockedLocally() == true)
                    throw Errors.AlreadyLocked();
                cancellationTask ??= cancellationToken.ToTask(true);
                await Task.WhenAny(existingLock.Task, cancellationTask).ConfigureAwait(false);
            }
            // Lock acquired, let's also mark it as local
            if (_localLock != null)
                _localLock.Value += 1;
            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New(state => {
                var (self, myLock1) = state;
                var oldLock = Interlocked.CompareExchange(ref self._lock, null, myLock1);
                if (oldLock == myLock1) {
                    var reentryCount = 0;
                    if (self._localLock != null) {
                        reentryCount = self._localLock.Value - 1;
                        self._localLock.Value = reentryCount;
                    }
                    if (reentryCount == 0)
                        myLock1.SetResult(default); // Must be done after setting _lock to null
                }
                return ValueTaskEx.Completed;
            }, (this, myLock));
        } 
    }

    public class AsyncLock<TKey> : IAsyncLock<TKey>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<Unit>> _locks = 
            new ConcurrentDictionary<TKey, TaskCompletionSource<Unit>>();
        private readonly AsyncLocal<Dictionary<TKey, int>>? _localLocks;

        public ReentryMode ReentryMode { get; }

        public AsyncLock(ReentryMode reentryMode)
        {
            ReentryMode = reentryMode;
            _localLocks = ReentryMode != ReentryMode.UncheckedDeadlock 
                ? new AsyncLocal<Dictionary<TKey, int>>() 
                : null;
        }
        public ValueTask<bool> IsLockedAsync(TKey key) 
            => new ValueTask<bool>(_locks.ContainsKey(key));

        public bool? IsLockedLocally(TKey key) 
            => _localLocks == null ? (bool?) null : (_localLocks.Value?.ContainsKey(key) ?? false);

        public void Prepare()
        {
            if (_localLocks != null)
                _localLocks.Value ??= new Dictionary<TKey, int>();
        }

        public async ValueTask<IAsyncDisposable> LockAsync(
            TKey key, CancellationToken cancellationToken = default)
        {
            var myLock = new TaskCompletionSource<Unit>();
            var cancellationTask = (Task?) null;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (_locks.TryAdd(key, myLock))
                    break;
                if (!_locks.TryGetValue(key, out var existingLock))
                    continue;
                if (IsLockedLocally(key) == true)
                    return CreateDisposable(key, myLock, true);
                cancellationTask ??= cancellationToken.ToTask(true);
                await Task.WhenAny(existingLock.Task, cancellationTask).ConfigureAwait(false);
                // No need to spin here: the probability of seeing another
                // lock in TryAdd and not seeing it in TryGetValue is nearly
                // zero (i.e. it was removed right between these calls).
            }
            return CreateDisposable(key, myLock, false);
        }

        protected IAsyncDisposable CreateDisposable(TKey key, TaskCompletionSource<Unit> myLock, bool hasLocalLock)
        {
            // Adding local lock
            var localLocks = _localLocks?.Value;
            if (localLocks != null) {
                var reentryCount = localLocks.GetValueOrDefault(key) + 1;
                localLocks[key] = reentryCount;
            }

            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New(state => {
                var (self, key2, myLock2) = state;
                // Removing local lock
                var reentryCount = 0;
                var localLocks = self._localLocks?.Value;
                if (localLocks != null) {
                    reentryCount = localLocks.GetValueOrDefault(key2) - 1;
                    localLocks[key2] = reentryCount;
                }
                if (reentryCount == 0)
                    myLock2.SetResult(default); // Must be done after TryRemove
                return ValueTaskEx.Completed;
            }, (this, key, myLock));
        }
    }
}
