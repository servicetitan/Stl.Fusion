namespace Stl.Async.Internal;

// Based on https://github.com/dotnet/runtime/issues/22144#issuecomment-1328319861

[StructLayout(LayoutKind.Auto)]
public readonly struct SilentTaskAwaiter<TTask> : ICriticalNotifyCompletion
    where TTask : Task
{
    private readonly TTask _task;
    private readonly bool _captureContext;

    public bool IsCompleted => _task.IsCompleted;

    public SilentTaskAwaiter(TTask task, bool captureContext = true)
    {
        _task = task;
        _captureContext = captureContext;
    }

    public SilentTaskAwaiter<TTask> GetAwaiter() => this;
    public void GetResult() { }

    public void OnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().OnCompleted(action);
    public void UnsafeOnCompleted(Action action)
        => _task.ConfigureAwait(_captureContext).GetAwaiter().UnsafeOnCompleted(action);
}
