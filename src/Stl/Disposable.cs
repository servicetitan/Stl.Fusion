namespace Stl;

/// <summary>
/// A set of helper methods related to <see cref="IDisposable"/>.
/// </summary>
public static class Disposable
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Disposable<Action> New(Action disposer)
        => new(disposer, action => action.Invoke());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Disposable<T> New<T>(T resource, Action<T> disposer)
        => new(resource, disposer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Disposable<T, TState> New<T, TState>(T resource, TState state, Action<T, TState> disposer)
        => new(resource, state, disposer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClosedDisposable<TState> NewClosed<TState>(TState state, Action<TState> disposer)
        => new(state, disposer);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static ClosedDisposable<(T1, T2)> Join<T1, T2>(T1 disposable1, T2 disposable2)
        where T1 : IDisposable
        where T2 : IDisposable
        => NewClosed<(T1, T2)>((disposable1, disposable2), state => {
            try {
                state.Item1?.Dispose();
            }
            finally {
                state.Item2?.Dispose();
            }
        });
}

public readonly struct Disposable<T> : IDisposable
{
    private readonly Action<T>? _disposer;

    public T Resource { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Disposable(T resource, Action<T> disposer)
    {
        Resource = resource;
        _disposer = disposer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _disposer?.Invoke(Resource);
}

public readonly struct Disposable<T, TState> : IDisposable
{
    private readonly Action<T, TState>? _disposer;
    private readonly TState _state;

    public T Resource { get; }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Disposable(T resource, TState state, Action<T, TState> disposer)
    {
        Resource = resource;
        _state = state;
        _disposer = disposer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _disposer?.Invoke(Resource, _state);
}

public readonly struct ClosedDisposable<TState> : IDisposable
{
    private readonly TState _state;
    private readonly Action<TState>? _disposer;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public ClosedDisposable(TState state, Action<TState> disposer)
    {
        _state = state;
        _disposer = disposer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Dispose() => _disposer?.Invoke(_state);
}
