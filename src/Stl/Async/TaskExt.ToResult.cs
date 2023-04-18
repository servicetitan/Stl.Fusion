namespace Stl.Async;

public static partial class TaskExt
{
    // ToResultSynchronously

    public static Result<Unit> ToResultSynchronously(this Task task)
        => task.AssertCompleted().IsCompletedSuccessfully()
            ? default
            : new Result<Unit>(default, task.GetBaseException());

    public static Result<T> ToResultSynchronously<T>(this Task<T> task)
        => task.AssertCompleted().IsCompletedSuccessfully()
            ? task.Result
            : new Result<T>(default!, task.GetBaseException());

    // ToResultAsync

    public static async Task<Result<Unit>> ToResultAsync(this Task task)
    {
        try {
            await task.ConfigureAwait(false);
            return default;
        }
        catch (Exception e) {
            return new Result<Unit>(default, e);
        }
    }

    public static async Task<Result<T>> ToResultAsync<T>(this Task<T> task)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            return new Result<T>(default!, e);
        }
    }

    // ToTypedResultXxx

    public static IResult ToTypedResultSynchronously(this Task task)
    {
        var tValue = task.AssertCompleted().GetType().GetTaskOrValueTaskArgument();
        if (tValue == null) {
            // ReSharper disable once HeapView.BoxingAllocation
            return task.IsCompletedSuccessfully()
                ? new Result<Unit>()
                : new Result<Unit>(default, task.GetBaseException());
        }

        return ToTypedResultCache.GetOrAdd(
            tValue,
            static tValue1 => (Func<Task, IResult>)FromTypedTaskInternalMethod
                .MakeGenericMethod(tValue1)
                .CreateDelegate(typeof(Func<Task, IResult>))
            ).Invoke(task);
    }

    public static Task<IResult> ToTypedResultAsync(this Task task)
        => task.ContinueWith(
            t => t.ToTypedResultSynchronously(),
            default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
}
