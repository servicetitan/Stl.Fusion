namespace Stl.Async;

public sealed class AsyncEvent<T>
{
    private readonly bool _runContinuationsAsynchronously;
    private readonly TaskCompletionSource<AsyncEvent<T>> _nextSource;

    public T Value { get; }

    public AsyncEvent(T value, bool runContinuationsAsynchronously)
    {
        _runContinuationsAsynchronously = runContinuationsAsynchronously;
        _nextSource = TaskCompletionSourceExt.New<AsyncEvent<T>>(runContinuationsAsynchronously);
        Value = value;
    }

    public override string ToString()
        => $"{GetType().GetName()}({Value})";

    public Task<AsyncEvent<T>> WhenNext()
        => _nextSource.Task;
    public Task<AsyncEvent<T>> WhenNext(CancellationToken cancellationToken)
        => _nextSource.Task.WaitAsync(cancellationToken);

    public AsyncEvent<T> SetNext(T value)
    {
        var next = new AsyncEvent<T>(value, _runContinuationsAsynchronously);
        _nextSource.TrySetResult(next);
        return next;
    }

    public void CancelNext(CancellationToken cancellationToken = default)
        => _nextSource.TrySetCanceled(cancellationToken);
}
