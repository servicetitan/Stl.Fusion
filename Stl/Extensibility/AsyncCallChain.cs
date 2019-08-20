using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Stl.Extensibility
{
    public interface IAsyncCallChain<TState> : IDisposable
    {
        TState State { get; set; }
        Task InvokeNextAsync();
    }
    
    public class AsyncCallChain<TState> : IAsyncCallChain<TState>
    {
        public IEnumerator<Func<IAsyncCallChain<TState>, Task>>? Tail { get; private set; }
        public TState State { get; set; }

        public AsyncCallChain(IEnumerable<Func<IAsyncCallChain<TState>, Task>> sequence, TState initialState)
            : this(sequence.GetEnumerator(), initialState) 
        { }

        public AsyncCallChain(IEnumerator<Func<IAsyncCallChain<TState>, Task>> tail, TState initialState)
        {
            Tail = tail;
            State = initialState;
        }

        public void Dispose()
        {
            var tail = Tail;
            Tail = null;
            tail?.Dispose();
        }

        public Task InvokeNextAsync()
        {
            var tail = Tail;
            if (tail == null)
                return Task.CompletedTask;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return Task.CompletedTask;
            }
            return tail.Current!.Invoke(this);
        }
    }
    
    public class AsyncCallChain<T, TState> : IAsyncCallChain<TState>
    {
        public IEnumerator<T>? Tail { get; private set; }
        public Func<T, IAsyncCallChain<TState>, Task> Handler { get; }
        public TState State { get; set; }

        public AsyncCallChain(IEnumerable<T> sequence, Func<T, IAsyncCallChain<TState>, Task> handler, TState initialState)
            : this(sequence.GetEnumerator(), handler, initialState) 
        { }

        public AsyncCallChain(IEnumerator<T> tail, Func<T, IAsyncCallChain<TState>, Task> handler, TState initialState)
        {
            Tail = tail;
            Handler = handler;
            State = initialState;
        }

        public void Dispose()
        {
            var tail = Tail;
            Tail = null;
            tail?.Dispose();
        }

        public Task InvokeNextAsync()
        {
            var tail = Tail;
            if (tail == null)
                return Task.CompletedTask;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return Task.CompletedTask;
            }
            return Handler.Invoke(tail.Current, this);
        }
    }
}