using Stl.Internal;

namespace Stl.Async;

#pragma warning disable MA0004

public static partial class TaskExt
{
    private static readonly MethodInfo FromTypedTaskInternalMethod =
        typeof(TaskExt).GetMethod(nameof(FromTypedTaskInternal), BindingFlags.Static | BindingFlags.NonPublic)!;
    private static readonly ConcurrentDictionary<Type, Func<Task, IResult>> ToTypedResultCache = new();

    public static readonly Task NeverEndingTask;
    public static readonly Task<Unit> NeverEndingUnitTask;
    public static readonly Task<Unit> UnitTask = Task.FromResult(Unit.Default);
    public static readonly Task<bool> TrueTask = Task.FromResult(true);
    public static readonly Task<bool> FalseTask = Task.FromResult(false);

    static TaskExt()
    {
        NeverEndingUnitTask = NeverEnding();
        NeverEndingTask = NeverEndingUnitTask;

        async Task<Unit> NeverEnding()
            => await TaskSource.New<Unit>(true).Task.ConfigureAwait(false);
    }

    // ToValueTask

    public static ValueTask<T> ToValueTask<T>(this Task<T> source) => new(source);
    public static ValueTask ToValueTask(this Task source) => new(source);

    // GetBaseException

    public static Exception GetBaseException(this Task task)
        => task.AssertCompleted().Exception?.GetBaseException()
            ?? (task.IsCanceled
                ? new TaskCanceledException(task)
                : throw Errors.TaskIsFaultedButNoExceptionAvailable());

    // AssertXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task AssertCompleted(this Task task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Task<T> AssertCompleted<T>(this Task<T> task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    // Private methods

    private static IResult FromTypedTaskInternal<T>(Task task)
        // ReSharper disable once HeapView.BoxingAllocation
        => task.IsCompletedSuccessfully()
            ? Result.Value(((Task<T>) task).Result)
            : Result.Error<T>(task.GetBaseException());
}
