using System.Threading;
using System.Threading.Tasks;

namespace Stl.Async
{
    public static class TaskSourceEx
    {
        public static void SetFromTask<T>(this TaskSource<T> target, Task<T> source)
        {
            if (source.IsCanceled)
                target.SetCanceled();
            else if (source.Exception != null)
                target.SetException(source.Exception);
            else
                target.SetResult(source.Result);
        }

        public static void TrySetFromTask<T>(this TaskSource<T> target, Task<T> source)
        {
            if (source.IsCanceled)
                target.TrySetCanceled();
            else if (source.Exception != null)
                target.TrySetException(source.Exception);
            else
                target.TrySetResult(source.Result);
        }

        // WithCancellation

        public static Task<T> WithCancellation<T>(this TaskSource<T> target, 
            CancellationToken cancellationToken)
        {
            var task = target.Task;
            if (task.IsCompleted)
                return task;
            if (cancellationToken != default) {
                cancellationToken.Register(arg => {
                    var target1 = (TaskSource<T>) arg;
                    target1.TrySetCanceled();
                }, target);
            }
            return task;
        }
    }
}
