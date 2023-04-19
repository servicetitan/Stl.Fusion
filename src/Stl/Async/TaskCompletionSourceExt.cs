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

    public static TaskCompletionSource<T> WithCancellation<T>(this TaskCompletionSource<T> value)
    {
        value.TrySetCanceled();
        return value;
    }

    public static TaskCompletionSource<T> WithCancellation<T>(this TaskCompletionSource<T> value, CancellationToken cancellationToken)
    {
        value.TrySetCanceled(cancellationToken);
        return value;
    }

    // (Try)SetFromTask

    public static void SetFromTask<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken candidateToken)
    {
        if (task.IsCanceled)
            target.SetCanceled();
        else if (task.Exception != null)
            target.SetException(task.Exception.GetBaseException());
        else
            target.SetResult(task.Result);
    }

    public static bool TrySetFromTask<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken candidateToken)
        => task.IsCanceled
            ? target.TrySetCanceled(candidateToken.IsCancellationRequested ? candidateToken : default)
            : task.Exception != null
                ? target.TrySetException(task.Exception.GetBaseException())
                : target.TrySetResult(task.Result);

    // (Try)SetFromTaskAsync

    public static Task SetFromTaskAsync<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken cancellationToken = default)
        => task.ContinueWith(
            t => target.SetFromTask(t, cancellationToken),
            default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public static Task TrySetFromTaskAsync<T>(this TaskCompletionSource<T> target, Task<T> task, CancellationToken cancellationToken = default)
        => task.ContinueWith(
            t => target.TrySetFromTask(t, cancellationToken),
            default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    // (Try)SetFromResult

    public static void SetFromResult<T>(this TaskCompletionSource<T> target, Result<T> result, CancellationToken candidateToken)
    {
        if (result.IsValue(out var v, out var e))
            target.SetResult(v);
        else if (e is OperationCanceledException && candidateToken.IsCancellationRequested)
            target.SetCanceled();
        else
            target.SetException(e);
    }

    public static bool TrySetFromResult<T>(this TaskCompletionSource<T> target, Result<T> result, CancellationToken candidateToken)
        => result.IsValue(out var v, out var e)
            ? target.TrySetResult(v)
            : e is OperationCanceledException && candidateToken.IsCancellationRequested
                ? target.TrySetCanceled(candidateToken)
                : target.TrySetException(e);
}
