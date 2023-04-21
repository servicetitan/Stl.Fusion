namespace Stl.Async;

public static class TaskCompletionSourceExt
{
    // NewXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource<T> New<T>()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource<T> NewSynchronous<T>()
        => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource<T> New<T>(bool runContinuationsAsynchronously)
        => runContinuationsAsynchronously
            ? new(TaskCreationOptions.RunContinuationsAsynchronously)
            : new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource<T> New<T>(TaskCreationOptions taskCreationOptions)
        => new(taskCreationOptions);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource<T> New<T>(object? state, TaskCreationOptions taskCreationOptions)
        => new(state, taskCreationOptions);

    // WithXxx

    public static TaskCompletionSource<T> WithResult<T>(this TaskCompletionSource<T> value, T result)
    {
        value.TrySetResult(result);
        return value;
    }

    public static TaskCompletionSource<T> WithException<T>(this TaskCompletionSource<T> value, Exception error)
    {
        value.TrySetException(error);
        return value;
    }

    public static TaskCompletionSource<T> WithCancellation<T>(this TaskCompletionSource<T> value, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            value.TrySetCanceled(cancellationToken);
        else
            value.TrySetCanceled();
        return value;
    }

    // (Try)SetFromTask

    public static void SetFromTask<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken cancellationToken = default)
    {
        if (task.IsCanceled) {
#if NET5_0_OR_GREATER
            if (cancellationToken.IsCancellationRequested)
                target.SetCanceled(cancellationToken);
            else
                // ReSharper disable once MethodSupportsCancellation
                target.SetCanceled();
#else
            target.SetCanceled();
#endif
        }
        else if (task.Exception != null)
            target.SetException(task.Exception.GetBaseException());
        else
            target.SetResult(task.Result);
    }

    public static bool TrySetFromTask<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken cancellationToken = default)
        => task.IsCanceled
            ? cancellationToken.IsCancellationRequested
                ? target.TrySetCanceled(cancellationToken)
                : target.TrySetCanceled()
            : task.Exception != null
                ? target.TrySetException(task.Exception.GetBaseException())
                : target.TrySetResult(task.Result);

    // (Try)SetFromTaskAsync

    public static Task SetFromTaskAsync<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken cancellationToken = default)
    {
        task.GetAwaiter().OnCompleted(() => target.SetFromTask(task, cancellationToken));
        return target.Task;
    }

    public static Task TrySetFromTaskAsync<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken cancellationToken = default)
    {
        task.GetAwaiter().OnCompleted(() => target.TrySetFromTask(task, cancellationToken));
        return target.Task;
    }

    // (Try)SetFromResult

    public static void SetFromResult<T>(this TaskCompletionSource<T> target, Result<T> result, CancellationToken cancellationToken = default)
    {
        if (result.IsValue(out var v, out var e))
            target.SetResult(v);
        else if (e is OperationCanceledException) {
#if NET5_0_OR_GREATER
            if (cancellationToken.IsCancellationRequested)
                target.SetCanceled(cancellationToken);
            else
                // ReSharper disable once MethodSupportsCancellation
                target.SetCanceled();
#else
            target.SetCanceled();
#endif
        }
        else
            target.SetException(e);
    }

    public static bool TrySetFromResult<T>(this TaskCompletionSource<T> target, Result<T> result, CancellationToken cancellationToken = default)
        => result.IsValue(out var v, out var e)
            ? target.TrySetResult(v)
            : e is OperationCanceledException
                ? cancellationToken.IsCancellationRequested
                    ? target.TrySetCanceled(cancellationToken)
                    : target.TrySetCanceled()
                : target.TrySetException(e);
}
