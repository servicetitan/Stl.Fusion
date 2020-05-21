using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class TaskCompletionSourceEx
    {
        // (Try)SetFromTask

        public static void SetFromTask<T>(this TaskCompletionSource<T> target, Task<T> source)
        {
            if (source.IsCanceled)
                target.SetCanceled();
            else if (source.Exception != null)
                target.SetException(source.Exception);
            else
                target.SetResult(source.Result);
        }

        public static void TrySetFromTask<T>(this TaskCompletionSource<T> target, Task<T> source)
        {
            if (source.IsCanceled)
                target.TrySetCanceled();
            else if (source.Exception != null)
                target.TrySetException(source.Exception);
            else
                target.TrySetResult(source.Result);
        }

        // (Try)SetFromResult

        public static void SetFromResult<T>(this TaskCompletionSource<T> target, Result<T> source, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                target.SetCanceled();
            else if (source.Error != null)
                target.SetException(source.Error);
            else
                target.SetResult(source.Value);
        }

        public static void TrySetFromResult<T>(this TaskCompletionSource<T> target, Result<T> source, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
                target.TrySetCanceled();
            else if (source.Error != null)
                target.TrySetException(source.Error);
            else
                target.TrySetResult(source.Value);
        }

        // WithCancellation

        public static Task<T> WithCancellation<T>(this TaskCompletionSource<T> target, 
            CancellationToken cancellationToken)
        {
            var task = target.Task;
            if (task.IsCompleted)
                return task;
            if (cancellationToken != default) {
                cancellationToken.Register(arg => {
                    var target1 = (TaskCompletionSource<T>) arg;
                    target1.TrySetCanceled();
                }, target);
            }
            return task;
        }
    }
}
