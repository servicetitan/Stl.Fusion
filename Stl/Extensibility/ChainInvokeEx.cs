using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Extensibility
{
    public static class ChainInvokeEx
    {
        public static void ChainInvoke<T>(
            this IEnumerable<T> sequence,
            Action<T, ICallChain<Unit>> handler)
            => sequence.ChainInvoke(default, handler);

        public static TState ChainInvoke<T, TState>(
            this IEnumerable<T> sequence,
            TState initialState,
            Action<T, ICallChain<TState>> handler)
        {
            using var chain = new CallChain<T, TState>(sequence, initialState, handler);
            chain.InvokeNext();
            return chain.State;
        }

        public static TState ChainInvoke<TState>(
            this IEnumerable<Action<ICallChain<TState>>> handlers, 
            TState initialState)
        {
            using var chain = new CallChain<TState>(handlers, initialState);
            chain.InvokeNext();
            return chain.State;
        }
        
        public static Task ChainInvokeAsync<T>(
            this IEnumerable<T> sequence,
            Func<T, IAsyncCallChain<Unit>, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
            => sequence.ChainInvokeAsync(default, handler, cancellationToken);

        public static async Task<TState> ChainInvokeAsync<T, TState>(
            this IEnumerable<T> sequence, 
            TState initialState,
            Func<T, IAsyncCallChain<TState>, CancellationToken, Task> handler,
            CancellationToken cancellationToken = default)
        {
            using var chain = new AsyncCallChain<T, TState>(sequence, initialState, handler);
            await chain.InvokeNextAsync(cancellationToken);
            return chain.State;
        }

        public static async Task<TState> ChainInvokeAsync<TState>(
            this IEnumerable<Func<IAsyncCallChain<TState>, CancellationToken, Task>> handlers, 
            TState initialState,
            CancellationToken cancellationToken = default)
        {
            using var chain = new AsyncCallChain<TState>(handlers, initialState);
            await chain.InvokeNextAsync(cancellationToken);
            return chain.State;
        }
    }
}
