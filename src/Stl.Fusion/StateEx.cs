using System;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Fusion
{
    public static class StateEx
    {
        public static async ValueTask<TState> UpdateAsync<TState>(
            this TState state, bool addDependency, CancellationToken cancellationToken = default)
            where TState : class, IState
        {
            await state.Computed.UpdateAsync(addDependency, cancellationToken).ConfigureAwait(false);
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
    }
}
