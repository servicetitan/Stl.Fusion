namespace Stl.Async;

public abstract class AsyncEvent<T>
{
    protected readonly bool RunContinuationsAsynchronously;
    protected readonly TaskSource<AsyncEvent<T>> WhenNextSource;

    public T Value { get; }

    protected AsyncEvent(T value, bool runContinuationsAsynchronously)
    {
        RunContinuationsAsynchronously = runContinuationsAsynchronously;
        WhenNextSource = TaskSource.New<AsyncEvent<T>>(runContinuationsAsynchronously);
        Value = value;
    }

    public override string ToString()
        => $"{GetType().Name}({Value})";

    public Task<AsyncEvent<T>> WhenNext()
        => WhenNextSource.Task;
}
