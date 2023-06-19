namespace Stl.Async;

public sealed class AsyncEvent<T>
{
    private readonly bool _runContinuationsAsynchronously;
    private readonly TaskCompletionSource<AsyncEvent<T>> _nextSource;

    public T Value { get; }
    public bool IsLatest => !_nextSource.Task.IsCompleted;
    public bool IsTerminal => _nextSource.Task.IsFaultedOrCancelled();

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

    public async Task<T> When(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        var current = this;
        while (!predicate.Invoke(current.Value))
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
        return current.Value;
    }

    public async IAsyncEnumerable<T> Changes([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = this;
        while (true) {
            yield return current.Value;
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
        }
        // ReSharper disable once IteratorNeverReturns
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
        return whenNext.IsCompleted ? whenNext.GetAwaiter().GetResult() : null;
    }

    public AsyncEvent<T> ThrowIfTerminal()
    {
        var whenNext = WhenNext();
        if (whenNext.IsFaultedOrCancelled())
            whenNext.GetAwaiter().GetResult(); // This should always throw
        return this;
    }

    public AsyncEvent<T> CreateNext(T value)
    {
        var next = new AsyncEvent<T>(value, _runContinuationsAsynchronously);
        _nextSource.SetResult(next);
        return next;
    }

    public void MakeTerminal(Exception error)
        => _nextSource.SetException(error);

    public void MakeTerminal(CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        _nextSource.SetCanceled(cancellationToken);
#else
        _nextSource.SetCanceled();
#endif
    }
}
