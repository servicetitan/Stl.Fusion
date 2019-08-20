using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stl.Extensibility
{
    public static class ChainInvokeEx
    {
        public static TState ChainInvoke<T, TState>(
            this IEnumerable<T> sequence, 
            Action<T, ICallChain<TState>> handler,
            TState initialState)
        {
            using var chain = new CallChain<T, TState>(sequence, handler, initialState);
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
        
        public static async Task<TState> ChainInvokeAsync<T, TState>(
            this IEnumerable<T> sequence, 
            Func<T, IAsyncCallChain<TState>, Task> handler,
            TState initialState)
        {
            using var chain = new AsyncCallChain<T, TState>(sequence, handler, initialState);
            await chain.InvokeNextAsync();
            return chain.State;
        }

        public static async Task<TState> ChainInvokeAsync<TState>(
            this IEnumerable<Func<IAsyncCallChain<TState>, Task>> handlers, 
            TState initialState)
        {
            using var chain = new AsyncCallChain<TState>(handlers, initialState);
            await chain.InvokeNextAsync();
            return chain.State;
        }
    }
}