using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Locking 
{
    public class AsyncLockBasedLockSet<TKey> : IAsyncLockSet<TKey>
        where TKey : notnull
    {
        private readonly IAsyncLock _lock;

        public ReentryMode ReentryMode => _lock.ReentryMode;
        public int AcquiredLockCount => _lock.IsLocked ? 1 : 0;

        public AsyncLockBasedLockSet(IAsyncLock @lock) => _lock = @lock;

        public bool IsLocked(TKey key) => _lock.IsLocked;
        public bool? IsLockedLocally(TKey key) => _lock.IsLockedLocally;

        public ValueTask<IDisposable> LockAsync(
            TKey key, CancellationToken cancellationToken = default)
            => _lock.LockAsync(cancellationToken);
    }
}
