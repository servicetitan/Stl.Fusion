namespace Stl.Async;

public static class AsyncDisposable
{
    public static AsyncDisposable<Func<ValueTask>> New(Func<ValueTask> disposeHandler)
        => new(func => func.Invoke(), disposeHandler);

    public static AsyncDisposable<TState> New<TState>(Func<TState, ValueTask> disposeHandler, TState state)
        => new(disposeHandler, state);
}

public readonly struct AsyncDisposable<TState> : IAsyncDisposable
{
    private readonly Func<TState, ValueTask> _disposeHandler;
    private readonly TState _state;

    public AsyncDisposable(Func<TState, ValueTask> disposeHandler, TState state)
    {
        _disposeHandler = disposeHandler;
        _state = state;
    }

    public ValueTask DisposeAsync()
        => _disposeHandler?.Invoke(_state) ?? ValueTaskExt.CompletedTask;
}
