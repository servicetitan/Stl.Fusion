using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Locking
{
    public enum ReentryMode
    {
        CheckedFail = 0,
        CheckedPass,
        UncheckedDeadlock,
    }

    public interface IAsyncLock
    {
        ReentryMode ReentryMode { get; }
        ValueTask<bool> IsLockedAsync();
        bool? IsLockedLocally();
        void Prepare();
        ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default);
    }

    public interface IAsyncLock<in TKey>
        where TKey : notnull
    {
        ReentryMode ReentryMode { get; }
        ValueTask<bool> IsLockedAsync(TKey key);
        bool? IsLockedLocally(TKey key);
        void Prepare();
        ValueTask<IAsyncDisposable> LockAsync(TKey key, CancellationToken cancellationToken = default);
    }
}
