namespace Stl.Async;

public sealed class AsyncEvent<T>
{
    private readonly bool _runContinuationsAsynchronously;
    private readonly TaskCompletionSource<AsyncEvent<T>?> _nextSource;

    public T Value { get; }
    public bool IsLatest => !_nextSource.Task.IsCompleted;

    public AsyncEvent(T value, bool runContinuationsAsynchronously)
    {
        _runContinuationsAsynchronously = runContinuationsAsynchronously;
        _nextSource = TaskCompletionSourceExt.New<AsyncEvent<T>?>(runContinuationsAsynchronously);
        Value = value;
    }

    public override string ToString()
        => $"{GetType().GetName()}({Value})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<AsyncEvent<T>?> WhenNext()
        => _nextSource.Task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<AsyncEvent<T>?> WhenNext(CancellationToken cancellationToken)
        => _nextSource.Task.WaitAsync(cancellationToken);

    public async Task<T> When(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        var current = this;
        while (!predicate.Invoke(current.Value)) {
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
            if (current == null)
                throw new AsyncEventSequenceCompletedException();
        }
        return current.Value;
    }

    public async IAsyncEnumerable<T> Changes([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = this;
        while (true) {
            yield return current.Value;
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
            if (current == null)
                break;
        }
        // ReSharper disable once IteratorNeverReturns
    }

    public async IAsyncEnumerable<AsyncEvent<T>> Events([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = this;
        while (true) {
            yield return current;
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
            if (current == null)
                break;
        }
        // ReSharper disable once IteratorNeverReturns
    }

    public AsyncEvent<T> Latest()
    {
        var current = this;
        while (true) {
            var whenNext = current.WhenNext();
            if (!whenNext.IsCompleted)
                return current;

            var next = whenNext.GetAwaiter().GetResult();
            if (next == null)
                return current;

            current = next;
        }
    }

    public AsyncEvent<T> LatestOrThrow()
    {
        var current = this;
        while (true) {
            var whenNext = current.WhenNext();
            if (!whenNext.IsCompleted)
                return current;

            current = whenNext.GetAwaiter().GetResult();
            if (current == null)
                throw new AsyncEventSequenceCompletedException();
        }
    }

    public AsyncEvent<T>? TryGetNext()
    {
        var whenNext = WhenNext();
        return whenNext.IsCompleted ? whenNext.GetAwaiter().GetResult() : null;
    }

    public AsyncEvent<T> AppendNext(T value)
    {
        var next = new AsyncEvent<T>(value, _runContinuationsAsynchronously);
        _nextSource.SetResult(next);
        return next;
    }

    public AsyncEvent<T> TryAppendNext(T value)
    {
        var next = new AsyncEvent<T>(value, _runContinuationsAsynchronously);
        return _nextSource.TrySetResult(next) ? next : this;
    }

    public void Complete()
        => _nextSource.SetResult(null);

    public void Complete(Exception error)
        => _nextSource.SetException(error);

    public void Complete(CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        _nextSource.SetCanceled(cancellationToken);
#else
        _nextSource.SetCanceled();
#endif
    }

    public bool TryComplete(Exception error)
        => _nextSource.TrySetException(error);

    public bool TryComplete(CancellationToken cancellationToken)
        => _nextSource.TrySetCanceled(cancellationToken);
}
