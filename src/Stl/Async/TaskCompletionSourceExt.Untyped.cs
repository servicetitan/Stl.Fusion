namespace Stl.Async;

#if NET5_0_OR_GREATER

public static partial class TaskCompletionSourceExt
{
    // NewXxx

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource New()
        => new(TaskCreationOptions.RunContinuationsAsynchronously);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource NewSynchronous()
        => new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource New(bool runContinuationsAsynchronously)
        => runContinuationsAsynchronously
            ? new(TaskCreationOptions.RunContinuationsAsynchronously)
            : new();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource New(TaskCreationOptions taskCreationOptions)
        => new(taskCreationOptions);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TaskCompletionSource New(object? state, TaskCreationOptions taskCreationOptions)
        => new(state, taskCreationOptions);

    // WithXxx

    public static TaskCompletionSource WithResult(this TaskCompletionSource value)
    {
        value.TrySetResult();
        return value;
    }

    public static TaskCompletionSource WithException(this TaskCompletionSource value, Exception error)
    {
        if (error is OperationCanceledException oce)
            value.TrySetCanceled(oce.CancellationToken);
        else
            value.TrySetException(error);
        return value;
    }

    public static TaskCompletionSource WithCancellation(this TaskCompletionSource value, CancellationToken cancellationToken = default)
    {
        if (cancellationToken.IsCancellationRequested)
            value.TrySetCanceled(cancellationToken);
        else
            value.TrySetCanceled();
        return value;
    }

    // (Try)SetFromTask

    public static void SetFromTask(this TaskCompletionSource target, Task task, CancellationToken cancellationToken = default)
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
        else if (task.Exception is { } exception)
            target.SetException(exception.GetBaseException());
        else
            target.SetResult();
    }

    public static bool TrySetFromTask(this TaskCompletionSource target, Task task, CancellationToken cancellationToken = default)
        => task.IsCanceled
            ? cancellationToken.IsCancellationRequested
                ? target.TrySetCanceled(cancellationToken)
                : target.TrySetCanceled()
            : task.Exception != null
                ? target.TrySetException(task.Exception.GetBaseException())
                : target.TrySetResult();

    // (Try)SetFromTaskAsync

    public static Task SetFromTaskAsync(this TaskCompletionSource target, Task task, CancellationToken cancellationToken = default)
    {
        _ = task.ContinueWith(
            t => target.SetFromTask(t, cancellationToken),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return target.Task;
    }

    public static Task TrySetFromTaskAsync(this TaskCompletionSource target, Task task, CancellationToken cancellationToken = default)
    {
        _ = task.ContinueWith(
            t => target.TrySetFromTask(t, cancellationToken),
            CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
        return target.Task;
    }

    // (Try)SetFromResult

    public static void SetFromResult(this TaskCompletionSource target, Result<Unit> result, CancellationToken cancellationToken = default)
    {
        if (result.IsValue(out var v, out var e))
            target.SetResult();
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

    public static bool TrySetFromResult(this TaskCompletionSource target, Result<Unit> result, CancellationToken cancellationToken = default)
        => result.IsValue(out var v, out var e)
            ? target.TrySetResult()
            : e is OperationCanceledException
                ? cancellationToken.IsCancellationRequested
                    ? target.TrySetCanceled(cancellationToken)
                    : target.TrySetCanceled()
                : target.TrySetException(e);
}

#endif
