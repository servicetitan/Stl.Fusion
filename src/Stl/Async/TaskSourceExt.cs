namespace Stl.Async;

public static class TaskSourceExt
{
    // (Try)SetFromTask

    public static void SetFromTask<T>(this TaskSource<T> target, Task<T> source, CancellationToken candidateToken)
    {
        if (source.IsCanceled)
            target.SetCanceled(candidateToken.IsCancellationRequested ? candidateToken : CancellationToken.None);
        else if (source.Exception != null)
            target.SetException(source.Exception);
        else
            target.SetResult(source.Result);
    }

    public static bool TrySetFromTask<T>(this TaskSource<T> target, Task<T> source, CancellationToken candidateToken)
        => source.IsCanceled
            ? target.TrySetCanceled(candidateToken.IsCancellationRequested ? candidateToken : CancellationToken.None)
            : source.Exception != null
                ? target.TrySetException(source.Exception)
                : target.TrySetResult(source.Result);

    public static void TrySetFromTaskWhenCompleted<T>(this TaskSource<T> target, Task<T> source, CancellationToken cancellationToken = default)
        => _ = source.ContinueWith(s => {
            target.TrySetFromTask(s, cancellationToken);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

    public static void TrySetFromTaskWhenCompleted(this TaskSource<Unit> target, Task source, CancellationToken cancellationToken = default)
        => _ = source.ContinueWith(_ => {
            if (source.IsCanceled)
                target.SetCanceled(cancellationToken.IsCancellationRequested ? cancellationToken : CancellationToken.None);
            else if (source.Exception != null)
                target.SetException(source.Exception);
            else
                target.SetResult(default);
        }, CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);

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

    // WithCancellation

    public static Task<T> WithCancellation<T>(this TaskSource<T> target,
        CancellationToken cancellationToken)
    {
        var task = target.Task;
        if (task.IsCompleted)
            return task;
        if (cancellationToken != default) {
            cancellationToken.Register(arg => {
                var target1 = (TaskSource<T>) arg!;
                target1.TrySetCanceled();
            }, target);
        }
        return task;
    }
}
