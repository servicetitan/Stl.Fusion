namespace Stl.Async.Internal;

// Based on https://github.com/dotnet/runtime/issues/22144#issuecomment-1328319861

[StructLayout(LayoutKind.Auto)]
public readonly struct VoidValueTaskAwaiter : ICriticalNotifyCompletion
{
    private readonly ValueTask _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public VoidValueTaskAwaiter(ValueTask task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public VoidValueTaskAwaiter GetAwaiter() => this;
    public void GetResult() { }

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}

[StructLayout(LayoutKind.Auto)]
public readonly struct VoidValueTaskAwaiter<T> : ICriticalNotifyCompletion
{
    private readonly ValueTask<T> _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public VoidValueTaskAwaiter(ValueTask<T> task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public VoidValueTaskAwaiter<T> GetAwaiter() => this;
    public void GetResult() { }

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}
