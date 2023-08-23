namespace Stl.Async;

public sealed class AsyncState<T>(T value, bool runContinuationsAsynchronously) : IAsyncEnumerable<AsyncState<T>>
{
    private readonly TaskCompletionSource<AsyncState<T>> _next
        = TaskCompletionSourceExt.New<AsyncState<T>>(runContinuationsAsynchronously);

    public T Value { get; } = value;
    public bool IsFinal => _next.Task.IsFaultedOrCancelled();
    public AsyncState<T>? Next => _next.Task.IsCompleted ? _next.Task.Result : null;

    public AsyncState<T> Last {
        get {
            var current = this;
            while (current.Next is { } next)
                current = next;
            return current;
        }
    }

    async IAsyncEnumerator<AsyncState<T>> IAsyncEnumerable<AsyncState<T>>.GetAsyncEnumerator(
        CancellationToken cancellationToken)
    {
        var current = this;
        while (true) {
            yield return current;
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    public override string ToString()
        => $"{GetType().GetName()}({Value})";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<AsyncState<T>> WhenNext()
        => _next.Task;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Task<AsyncState<T>> WhenNext(CancellationToken cancellationToken)
        => _next.Task.WaitAsync(cancellationToken);

    public async Task<T> When(Func<T, bool> predicate, CancellationToken cancellationToken = default)
    {
        var current = this;
        while (!predicate.Invoke(current.Value))
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
        return current.Value;
    }

    public async IAsyncEnumerable<T> Changes(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var current = this;
        while (true) {
            yield return current.Value;
            current = await current.WhenNext(cancellationToken).ConfigureAwait(false);
        }
        // ReSharper disable once IteratorNeverReturns
    }

    // SetNext & TrySetNext

    public AsyncState<T> SetNext(T value)
    {
        var next = new AsyncState<T>(value, runContinuationsAsynchronously);
        _next.SetResult(next);
        return next;
    }

    public AsyncState<T> TrySetNext(T value)
    {
        var next = new AsyncState<T>(value, runContinuationsAsynchronously);
        return _next.TrySetResult(next) ? next : this;
    }

    // SetFinal & TrySetFinal

    public void SetFinal(Exception error)
        => _next.SetException(error);

    public void SetFinal(CancellationToken cancellationToken)
    {
#if NET5_0_OR_GREATER
        _next.SetCanceled(cancellationToken);
#else
        _next.SetCanceled();
#endif
    }

    public bool TrySetFinal(Exception error)
        => _next.TrySetException(error);

    public bool TrySetFinal(CancellationToken cancellationToken)
        => _next.TrySetCanceled(cancellationToken);
}
