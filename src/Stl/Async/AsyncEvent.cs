namespace Stl.Async;

public sealed class AsyncEvent<T>
{
    private readonly bool _runContinuationsAsynchronously;
    private readonly TaskCompletionSource<AsyncEvent<T>> _nextSource;

    public T Value { get; }
    public bool IsLatest => !_nextSource.Task.IsCompleted;

    public AsyncEvent(T value, bool runContinuationsAsynchronously)
    {
        _runContinuationsAsynchronously = runContinuationsAsynchronously;
        _nextSource = TaskCompletionSourceExt.New<AsyncEvent<T>>(runContinuationsAsynchronously);
        Value = value;
    }

    public override string ToString()
        => $"{GetType().GetName()}({Value})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<AsyncEvent<T>> WhenNext()
        => _nextSource.Task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<AsyncEvent<T>> WhenNext(CancellationToken cancellationToken)
        => _nextSource.Task.WaitAsync(cancellationToken);

    public async Task<AsyncEvent<T>> When(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        var current = this;
        while (!predicate.Invoke(current.Value))
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
        return current;
    }

    public AsyncEvent<T> GetLatest()
    {
        var current = this;
        while (true) {
            var whenNext = current.WhenNext();
            if (!whenNext.IsCompleted)
                return current;

            current = whenNext.GetAwaiter().GetResult();
        }
    }

    public AsyncEvent<T>? TryGetNext()
    {
        var whenNext = WhenNext();
        if (whenNext.IsCompleted)
            return whenNext.GetAwaiter().GetResult();

        return null;
    }

    public AsyncEvent<T> CreateNext(T value)
    {
        var next = new AsyncEvent<T>(value, _runContinuationsAsynchronously);
        _nextSource.SetResult(next);
        return next;
    }

    public void CancelNext(CancellationToken cancellationToken = default)
    {
#if NET5_0_OR_GREATER
        _nextSource.SetCanceled(cancellationToken);
#else
        _nextSource.SetCanceled();
#endif
    }
}
