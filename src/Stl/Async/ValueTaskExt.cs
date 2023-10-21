using Stl.Internal;

namespace Stl.Async;

#pragma warning disable CA2012

public static class ValueTaskExt
{
    public static readonly ValueTask NeverEndingTask = TaskExt.NeverEndingTask.ToValueTask();
    public static readonly ValueTask<Unit> NeverEndingUnitTask = TaskExt.NeverEndingUnitTask.ToValueTask();
    public static readonly ValueTask CompletedTask = Task.CompletedTask.ToValueTask();
    public static readonly ValueTask<bool> TrueTask = FromResult(true);
    public static readonly ValueTask<bool> FalseTask = FromResult(false);

    public static ValueTask<T> FromResult<T>(T value) => new(value);
    public static ValueTask<T> FromException<T>(Exception error) => new(Task.FromException<T>(error));

    public static T ResultOrThrow<T>(this ValueTask<T> task)
        => task.IsCompleted ? task.Result : throw Errors.TaskIsNotCompleted();

    // ToResultSynchronously

    public static Result<Unit> ToResultSynchronously(this ValueTask task)
        => task.AssertCompleted().IsCompletedSuccessfully
            ? default
            : new Result<Unit>(default, task.AsTask().GetBaseException());

    public static Result<T> ToResultSynchronously<T>(this ValueTask<T> task)
        => task.AssertCompleted().IsCompletedSuccessfully
            ? task.Result
            : new Result<T>(default!, task.AsTask().GetBaseException());

    // ToResultAsync

    public static async ValueTask<Result<Unit>> ToResultAsync(this ValueTask task)
    {
        try {
            await task.ConfigureAwait(false);
            return default;
        }
        catch (Exception e) {
            return new Result<Unit>(default, e);
        }
    }

    public static async ValueTask<Result<T>> ToResultAsync<T>(this ValueTask<T> task)
    {
        try {
            return await task.ConfigureAwait(false);
        }
        catch (Exception e) {
            return new Result<T>(default!, e);
        }
    }


    // WaitErrorAsync

    public static async ValueTask<Exception?> WaitErrorAsync(this ValueTask task, bool throwOperationCancelledException = false)
    {
        try {
            await task.ConfigureAwait(false);
            return null;
        }
        catch (Exception error) {
            if (throwOperationCancelledException && error is OperationCanceledException)
                throw;
            return error;
        }
    }

    public static async ValueTask<Exception?> WaitErrorAsync<T>(this ValueTask<T> task, bool throwOperationCancelledException = false)
    {
        try {
            await task.ConfigureAwait(false);
            return null;
        }
        catch (Exception error) {
            if (throwOperationCancelledException && error is OperationCanceledException)
                throw;
            return error;
        }
    }

    // AssertXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask AssertCompleted(this ValueTask task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ValueTask<T> AssertCompleted<T>(this ValueTask<T> task)
        => !task.IsCompleted ? throw Errors.TaskIsNotCompleted() : task;
}
