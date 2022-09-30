namespace Stl.Async;

public sealed class ManualAsyncEvent<T> : AsyncEvent<T>
{
    public ManualAsyncEvent(T value, bool runContinuationsAsynchronously)
        : base(value, runContinuationsAsynchronously)
    { }

    public ManualAsyncEvent<T> Create(T value)
        => new(value, RunContinuationsAsynchronously);

    public void SetNext(AsyncEvent<T> next)
        => WhenNextSource.TrySetResult(next);

    public void CancelNext(CancellationToken cancellationToken)
        => WhenNextSource.TrySetCanceled(cancellationToken);
}
