namespace Stl.Locking;

public static class SemaphoreSlimExt
{
    public static ValueTask<ClosedDisposable<SemaphoreSlim>> Lock(
        this SemaphoreSlim semaphore,
        CancellationToken cancellationToken = default)
    {
        var task = semaphore.WaitAsync(cancellationToken);
        return task.IsCompletedSuccessfully()
            ? ValueTaskExt.FromResult(new ClosedDisposable<SemaphoreSlim>(semaphore, static x => x.Release()))
            : CompleteAsynchronously(task, semaphore);

        static async ValueTask<ClosedDisposable<SemaphoreSlim>> CompleteAsynchronously(Task task1, SemaphoreSlim semaphore)
        {
            await task1.ConfigureAwait(false);
            return new ClosedDisposable<SemaphoreSlim>(semaphore, static x => x.Release());
        }
    }
}
