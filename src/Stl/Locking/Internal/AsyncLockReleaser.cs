namespace Stl.Locking.Internal;

public readonly struct AsyncLockReleaser : IDisposable
{
    public static ValueTask<AsyncLockReleaser> NewWhenCompleted(Task task, IAsyncLock asyncLock)
    {
        return task.IsCompletedSuccessfully()
            ? ValueTaskExt.FromResult(new AsyncLockReleaser(asyncLock))
            : CompleteAsynchronously(task, asyncLock);

        static async ValueTask<AsyncLockReleaser> CompleteAsynchronously(Task task1, IAsyncLock asyncLock1)
        {
            await task1.ConfigureAwait(false);
            return new AsyncLockReleaser(asyncLock1);
        }
    }

    public static ValueTask<AsyncLockReleaser> NewWhenCompleted<T>(ValueTask<T> task, IAsyncLock asyncLock)
    {
        return task.IsCompletedSuccessfully
            ? ValueTaskExt.FromResult(new AsyncLockReleaser(asyncLock))
            : CompleteAsynchronously(task, asyncLock);

        static async ValueTask<AsyncLockReleaser> CompleteAsynchronously(ValueTask<T> task1, IAsyncLock asyncLock1)
        {
            await task1.ConfigureAwait(false);
            return new AsyncLockReleaser(asyncLock1);
        }
    }

    private readonly IAsyncLock? _asyncLock;

    public AsyncLockReleaser(IAsyncLock? asyncLock)
        => _asyncLock = asyncLock;

    public void Dispose() => _asyncLock?.Release();
}
