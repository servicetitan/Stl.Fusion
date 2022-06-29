using Stl.Internal;

namespace Stl.Async;

public static class ValueTaskExt
{
    public static ValueTask NeverEndingTask { get; } = TaskExt.NeverEndingTask.ToValueTask();
    public static ValueTask<Unit> NeverEndingUnitTask { get; } = TaskExt.NeverEndingUnitTask.ToValueTask();
    public static ValueTask CompletedTask { get;  } = Task.CompletedTask.ToValueTask();
    public static ValueTask<bool> TrueTask { get;  } = FromResult(true);
    public static ValueTask<bool> FalseTask { get;  } = FromResult(false);

    public static ValueTask<T> FromResult<T>(T value) => new(value);
    public static ValueTask<T> FromException<T>(Exception error) => new(Task.FromException<T>(error));

    public static T ResultOrThrow<T>(this ValueTask<T> task)
        => task.IsCompleted ? task.Result : throw Errors.TaskIsNotCompleted();
}
