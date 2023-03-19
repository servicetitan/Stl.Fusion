namespace Stl.Async.Internal;

// Based on https://github.com/dotnet/runtime/issues/22144#issuecomment-1328319861

[StructLayout(LayoutKind.Auto)]
public readonly struct SuppressCancellationValueTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly ValueTask _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public SuppressCancellationValueTaskAwaiter(ValueTask task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public SuppressCancellationValueTaskAwaiter GetAwaiter() => this;

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
public readonly struct SuppressCancellationValueTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly ValueTask<T> _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public SuppressCancellationValueTaskAwaiter(ValueTask<T> task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public SuppressCancellationValueTaskAwaiter<T> GetAwaiter() => this;
    public T GetResult() => _task.IsCanceled ? default! : _task.GetAwaiter().GetResult();

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}
