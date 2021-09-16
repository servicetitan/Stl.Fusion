using System;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public static class ValueTaskExt
    {
        public static ValueTask CompletedTask { get;  } = Task.CompletedTask.ToValueTask();
        public static ValueTask<bool> TrueTask { get;  } = FromResult(true);
        public static ValueTask<bool> FalseTask { get;  } = FromResult(false);

        public static ValueTask<T> FromResult<T>(T value) => new ValueTask<T>(value);
        public static ValueTask<T> FromException<T>(Exception error)
            => new ValueTask<T>(Task.FromException<T>(error));

        public static T ResultOrThrow<T>(this ValueTask<T> task) =>
            task.IsCompleted ? task.Result : throw Errors.TaskIsNotCompleted();
    }
}
