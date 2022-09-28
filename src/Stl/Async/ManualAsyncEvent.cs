namespace Stl.Async;

public sealed class ManualAsyncEvent<T> : AsyncEvent<T>
{
    public ManualAsyncEvent(T value, bool runContinuationsAsynchronously)
        : base(value, runContinuationsAsynchronously)
    { }

    public ManualAsyncEvent<T> CreateNext(T value)
        => new(value, RunContinuationsAsynchronously);

    public void Complete(AsyncEvent<T> next)
        => WhenNextSource.TrySetResult(next);
}
