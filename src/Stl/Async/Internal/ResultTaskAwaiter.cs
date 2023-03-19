namespace Stl.Async.Internal;

// Based on https://github.com/dotnet/runtime/issues/22144#issuecomment-1328319861

[StructLayout(LayoutKind.Auto)]
public readonly struct ResultTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly Task _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public ResultTaskAwaiter(Task task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public ResultTaskAwaiter GetAwaiter() => this;
    public Result<Unit> GetResult() => _task.ToResultSynchronously();

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}

[StructLayout(LayoutKind.Auto)]
public readonly struct ResultTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly Task<T> _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public ResultTaskAwaiter(Task<T> task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public ResultTaskAwaiter<T> GetAwaiter() => this;
    public Result<T> GetResult() => _task.ToResultSynchronously();

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}
