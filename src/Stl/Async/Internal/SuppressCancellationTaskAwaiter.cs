namespace Stl.Async.Internal;

// Based on https://github.com/dotnet/runtime/issues/22144#issuecomment-1328319861

[StructLayout(LayoutKind.Auto)]
public readonly struct SuppressCancellationTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly Task _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public SuppressCancellationTaskAwaiter(Task task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public SuppressCancellationTaskAwaiter GetAwaiter() => this;

    public void GetResult()
    {
        if (_task.IsCanceled)
            return;
        _task.GetAwaiter().GetResult();
    }

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}

[StructLayout(LayoutKind.Auto)]
public readonly struct SuppressCancellationTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly Task<T> _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public SuppressCancellationTaskAwaiter(Task<T> task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public SuppressCancellationTaskAwaiter<T> GetAwaiter() => this;
    public T GetResult() => _task.IsCanceled ? default! : _task.GetAwaiter().GetResult();

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}
