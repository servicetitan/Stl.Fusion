using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Locking
{
    public interface IAsyncLockSet<in TKey>
        where TKey : notnull
    {
        ReentryMode ReentryMode { get; }
        int AcquiredLockCount { get; }
        bool IsLocked(TKey key);
        bool? IsLockedLocally(TKey key);
        ValueTask<IDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
