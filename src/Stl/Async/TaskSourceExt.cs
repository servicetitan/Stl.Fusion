namespace Stl.Async;

public static class TaskSourceExt
{
    // (Try)SetFromTask

    public static void SetFromTask<T>(this TaskSource<T> target, Task<T> source, CancellationToken candidateToken)
    {
        if (source.IsCanceled)
            target.SetCanceled(candidateToken.IsCancellationRequested ? candidateToken : default);
        else if (source.Exception != null)
            target.SetException(source.Exception.GetBaseException());
        else
            target.SetResult(source.Result);
    }

    public static bool TrySetFromTask<T>(this TaskSource<T> target, Task<T> source, CancellationToken candidateToken)
        => source.IsCanceled
            ? target.TrySetCanceled(candidateToken.IsCancellationRequested ? candidateToken : default)
            : source.Exception != null
                ? target.TrySetException(source.Exception.GetBaseException())
                : target.TrySetResult(source.Result);

    // TrySetFromTaskAsync

    public static Task TrySetFromTaskAsync<T>(this TaskSource<T> target, Task<T> source, CancellationToken cancellationToken = default)
        => source.ContinueWith(
            s => target.TrySetFromTask(s, cancellationToken),
            default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public static Task TrySetFromTaskAsync(this TaskSource<Unit> target, Task source, CancellationToken cancellationToken = default)
        => source.ContinueWith(_ => {
            if (source.IsCanceled)
                target.SetCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : default);
            else if (source.Exception != null)
                target.SetException(source.Exception.GetBaseException());
            else
                target.SetResult(default);
        }, default, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    // (Try)SetFromResult

    public static void SetFromResult<T>(this TaskSource<T> target, Result<T> source, CancellationToken candidateToken)
    {
        if (source.IsValue(out var v, out var e))
            target.SetResult(v);
        else if (e is OperationCanceledException && candidateToken.IsCancellationRequested)
            target.SetCanceled(candidateToken);
        else
            target.SetException(e);
    }

    public static bool TrySetFromResult<T>(this TaskSource<T> target, Result<T> source, CancellationToken candidateToken)
        => source.IsValue(out var v, out var e)
            ? target.TrySetResult(v)
            : e is OperationCanceledException && candidateToken.IsCancellationRequested
                ? target.TrySetCanceled(candidateToken)
                : target.TrySetException(e);
}
