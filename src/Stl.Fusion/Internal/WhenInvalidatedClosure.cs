namespace Stl.Fusion.Internal;

internal sealed class WhenInvalidatedClosure
{
    private readonly Action<IComputed> _onInvalidatedHandler;
    private readonly TaskCompletionSource<Unit> _taskSource;
    private readonly IComputed _computed;
    private readonly CancellationTokenRegistration _cancellationTokenRegistration;

    public Task Task => _taskSource.Task;

    internal WhenInvalidatedClosure(TaskCompletionSource<Unit> taskSource, IComputed computed, CancellationToken cancellationToken)
    {
        _taskSource = taskSource;
        _computed = computed;
        _onInvalidatedHandler = OnInvalidated;
        _computed.Invalidated += _onInvalidatedHandler;
        _cancellationTokenRegistration = cancellationToken.Register(OnUnregister);
    }

    private void OnInvalidated(IComputed _)
    {
        _taskSource.TrySetResult(default);
        _cancellationTokenRegistration.Dispose();
    }

    private void OnUnregister()
    {
        _taskSource.TrySetCanceled();
        _computed.Invalidated -= _onInvalidatedHandler;
    }
}
