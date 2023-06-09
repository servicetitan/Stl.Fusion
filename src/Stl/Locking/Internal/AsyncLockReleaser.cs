namespace Stl.Locking.Internal;

public readonly struct AsyncLockReleaser : IDisposable
{
    public static ValueTask<AsyncLockReleaser> NewWhenCompleted(Task task, AsyncLock asyncLock)
    {
        return task.IsCompletedSuccessfully()
            ? ValueTaskExt.FromResult(new AsyncLockReleaser(asyncLock))
            : CompleteAsynchronously(task, asyncLock);

        static async ValueTask<AsyncLockReleaser> CompleteAsynchronously(Task task1, AsyncLock asyncLock1)
        {
            await task1.ConfigureAwait(false);
            return new AsyncLockReleaser(asyncLock1);
        }
    }

    public static ValueTask<AsyncLockReleaser> NewWhenCompleted<T>(ValueTask<T> task, AsyncLock asyncLock)
    {
        return task.IsCompletedSuccessfully
            ? ValueTaskExt.FromResult(new AsyncLockReleaser(asyncLock))
            : CompleteAsynchronously(task, asyncLock);

        static async ValueTask<AsyncLockReleaser> CompleteAsynchronously(ValueTask<T> task1, AsyncLock asyncLock1)
        {
            await task1.ConfigureAwait(false);
            return new AsyncLockReleaser(asyncLock1);
        }
    }

    private readonly AsyncLock? _asyncLock;

    public AsyncLockReleaser(AsyncLock? asyncLock)
        => _asyncLock = asyncLock;

    public void Dispose() => _asyncLock?.Release();
}
