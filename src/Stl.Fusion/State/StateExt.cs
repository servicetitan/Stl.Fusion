namespace Stl.Fusion;

public static class StateExt
{
    // Computed-like methods

    public static ValueTask<T> Use<T>(
        this IState<T> state, CancellationToken cancellationToken = default)
        => state.Computed.Use(cancellationToken);

    public static void Invalidate(this IState state, bool immediately = false)
        => state.Computed.Invalidate(immediately);

    public static async ValueTask<TState> Update<TState>(
        this TState state, CancellationToken cancellationToken = default)
        where TState : class, IState
    {
        await state.Computed.Update(cancellationToken).ConfigureAwait(false);
        return state;
    }

    public static async ValueTask<TState> Recompute<TState>(
        this TState state, CancellationToken cancellationToken = default)
        where TState : class, IState
    {
        var snapshot = state.Snapshot;
        var computed = snapshot.Computed;
        computed.Invalidate();
        await computed.Update(cancellationToken).ConfigureAwait(false);
        return state;
    }

    // Add/RemoveEventHandler

    public static void AddEventHandler(this IState state,
        StateEventKind eventFilter, Action<IState, StateEventKind> handler)
    {
        if ((eventFilter & StateEventKind.Invalidated) != 0)
            state.Invalidated += handler;
        if ((eventFilter & StateEventKind.Updating) != 0)
            state.Updating += handler;
        if ((eventFilter & StateEventKind.Updated) != 0)
            state.Updated += handler;
    }

    public static void AddEventHandler<T>(this IState<T> state,
        StateEventKind eventFilter, Action<IState<T>, StateEventKind> handler)
    {
        if ((eventFilter & StateEventKind.Invalidated) != 0)
            state.Invalidated += handler;
        if ((eventFilter & StateEventKind.Updating) != 0)
            state.Updating += handler;
        if ((eventFilter & StateEventKind.Updated) != 0)
            state.Updated += handler;
    }

    public static void RemoveEventHandler(this IState state,
        StateEventKind eventFilter, Action<IState, StateEventKind> handler)
    {
        if ((eventFilter & StateEventKind.Invalidated) != 0)
            state.Invalidated -= handler;
        if ((eventFilter & StateEventKind.Updating) != 0)
            state.Updating -= handler;
        if ((eventFilter & StateEventKind.Updated) != 0)
            state.Updated -= handler;
    }

    public static void RemoveEventHandler<T>(this IState<T> state,
        StateEventKind eventFilter, Action<IState<T>, StateEventKind> handler)
    {
        if ((eventFilter & StateEventKind.Invalidated) != 0)
            state.Invalidated -= handler;
        if ((eventFilter & StateEventKind.Updating) != 0)
            state.Updating -= handler;
        if ((eventFilter & StateEventKind.Updated) != 0)
            state.Updated -= handler;
    }

    // When

    public static Task<Computed<T>> When<T>(this IState<T> state,
        Func<T, bool> predicate,
        CancellationToken cancellationToken = default)
        => state.Computed.When(predicate, cancellationToken);
    public static Task<Computed<T>> When<T>(this IState<T> state,
        Func<T, bool> predicate,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
        => state.Computed.When(predicate, updateDelayer, cancellationToken);

    // Changes

    public static IAsyncEnumerable<Computed<T>> Changes<T>(
        this IState<T> state,
        CancellationToken cancellationToken = default)
        => state.Computed.Changes(cancellationToken);
    public static IAsyncEnumerable<Computed<T>> Changes<T>(
        this IState<T> state,
        IUpdateDelayer updateDelayer,
        CancellationToken cancellationToken = default)
        => state.Computed.Changes(updateDelayer, cancellationToken);
}
