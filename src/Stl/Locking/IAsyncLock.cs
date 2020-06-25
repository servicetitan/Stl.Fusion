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
        bool IsLocked { get; }
        bool? IsLockedLocally { get; }
        ValueTask<IDisposable> LockAsync(CancellationToken cancellationToken = default);
    }
}
