namespace Stl.Async;

public static class AsyncDisposable
{
    public static AsyncDisposable<Func<ValueTask>> New(Func<ValueTask> disposeHandler)
        => new(static func => func.Invoke(), disposeHandler);

    public static AsyncDisposable<TState> New<TState>(Func<TState, ValueTask> disposeHandler, TState state)
        => new(disposeHandler, state);
}

public readonly struct AsyncDisposable<TState>(Func<TState, ValueTask>? disposeHandler, TState state)
    : IAsyncDisposable
{
    public ValueTask DisposeAsync()
        => disposeHandler?.Invoke(state) ?? default;
}
