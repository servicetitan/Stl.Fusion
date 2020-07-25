using System;
using System.Threading.Tasks;

namespace Stl.Testing
{
    public static class TaskEx
    {
        public static Func<Task> AsAsyncFunc(this Task task)
            => () => task;
        public static Func<Task<T>> AsAsyncFunc<T>(this Task<T> task)
            => () => task;
        public static Func<Task> AsAsyncFunc(this ValueTask task)
            => task.AsTask;
        public static Func<Task<T>> AsAsyncFunc<T>(this ValueTask<T> task)
            => task.AsTask;
    }
}
