using System;
using System.Collections.Concurrent;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Locking 
{
    public class InProcessLock : ILock
    {
        private volatile TaskCompletionSource<Unit>? _lock = null;

        public ValueTask<bool> IsLockedAsync() 
            => new ValueTask<bool>(_lock != null); 

        public async ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
        {
            var myLock = new TaskCompletionSource<Unit>();
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var existingLock = Interlocked.CompareExchange(ref _lock, myLock, null);
                if (existingLock == null)
                    break;
                await Task.WhenAny(existingLock.Task, cancellationToken.ToTask(true));
            }
            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New(state => {
                var (self, myLock1) = state;
                var oldLock = Interlocked.CompareExchange(ref self._lock, null, myLock1);
                if (oldLock == myLock1)
                    myLock1.SetResult(default); // Must be done after setting _lock to null
                return Task.CompletedTask.ToValueTask();
            }, (this, myLock));
        } 
    }

    public class InProcessLock<TKey> : ILock<TKey>
        where TKey : notnull
    {
        private readonly ConcurrentDictionary<TKey, TaskCompletionSource<Unit>> _locks = 
            new ConcurrentDictionary<TKey, TaskCompletionSource<Unit>>();

        public ValueTask<bool> IsLockedAsync(TKey key) => 
            new ValueTask<bool>(_locks.ContainsKey(key));

        public async ValueTask<IAsyncDisposable> LockAsync(
            TKey key, CancellationToken cancellationToken = default)
        {
            var myLock = new TaskCompletionSource<Unit>();
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                if (_locks.TryAdd(key, myLock))
                    break;
                if (!_locks.TryGetValue(key, out var existingLock))
                    // No need to spin here: the probability of seeing another
                    // lock in TryAdd and not seeing it in TryGetValue is nearly
                    // zero (i.e. it was removed right between these calls).
                    continue; 
                await Task.WhenAny(existingLock.Task, cancellationToken.ToTask(true));
            }
            // ReSharper disable once HeapView.BoxingAllocation
            return Disposable.New(state => {
                var (self, key1, myLock1) = state;
                self._locks.TryRemove(key1, out _);
                myLock1.SetResult(default); // Must be done after TryRemove
                return Task.CompletedTask.ToValueTask();
            }, (this, key, myLock));
        } 
    }

    public class InProcessGlobalLock<TKey> : ILock<TKey>
        where TKey : notnull
    {
        private readonly InProcessLock _lock = new InProcessLock();

        public ValueTask<bool> IsLockedAsync(TKey key) => _lock.IsLockedAsync();

        public ValueTask<IAsyncDisposable> LockAsync(
            TKey key, CancellationToken cancellationToken = default)
            => _lock.LockAsync(cancellationToken);
    }
}
