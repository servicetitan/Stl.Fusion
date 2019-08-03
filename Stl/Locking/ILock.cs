using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Locking
{
    public interface ILock
    {
        ValueTask<bool> IsLockedAsync();
        ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default);
    }

    public interface ILock<in TKey>
        where TKey : notnull
    {
        ValueTask<bool> IsLockedAsync(TKey key);
        ValueTask<IAsyncDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
