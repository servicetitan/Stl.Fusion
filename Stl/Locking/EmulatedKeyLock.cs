using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Locking 
{
    public class FakeKeyLock<TKey> : IAsyncLock<TKey>
        where TKey : notnull
    {
        private readonly IAsyncLock _lock;

        public ReentryMode ReentryMode => _lock.ReentryMode;

        public FakeKeyLock(IAsyncLock @lock) => _lock = @lock;

        public ValueTask<bool> IsLockedAsync(TKey key) => _lock.IsLockedAsync();

        public bool? IsLockedLocally(TKey key) => _lock.IsLockedLocally();

        public void Prepare() => _lock.Prepare();

        public ValueTask<IAsyncDisposable> LockAsync(
            TKey key, CancellationToken cancellationToken = default)
            => _lock.LockAsync(cancellationToken);
    }
}
