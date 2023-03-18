using System.Runtime.ExceptionServices;

namespace Stl.Async;

public static partial class TaskExt
{
    public static async Task SuppressExceptions(this Task task, Func<Exception, bool>? filter = null)
    {
        try {
            await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            if (filter?.Invoke(e) ?? true)
                return;
            throw;
        }
    }

    public static async Task<T> SuppressExceptions<T>(this Task<T> task, Func<Exception, bool>? filter = null)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            if (filter?.Invoke(e) ?? true)
                return default!;
            throw;
        }
    }

    public static Task SuppressCancellation(this Task task)
        => task.ContinueWith(
            t => {
                if (t.IsCompletedSuccessfully() || t.IsCanceled)
                    return;
                ExceptionDispatchInfo.Capture(t.Exception!.GetBaseException()).Throw();
            },
            default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public static Task<T> SuppressCancellation<T>(this Task<T> task)
        => task.ContinueWith(
            t => t.IsCanceled ? default! : t.Result,
            default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
}
