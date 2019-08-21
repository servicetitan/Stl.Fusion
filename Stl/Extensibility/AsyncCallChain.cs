using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Extensibility
{
    public interface IAsyncCallChain<TState> : IDisposable
    {
        TState State { get; set; }
        Task InvokeNextAsync(CancellationToken cancellationToken);
    }
    
    public class AsyncCallChain<TState> : IAsyncCallChain<TState>
    {
        public IEnumerator<Func<IAsyncCallChain<TState>, CancellationToken, Task>>? Tail { get; private set; }
        public TState State { get; set; }
        public int Position { get; private set; }

        public AsyncCallChain(IEnumerable<Func<IAsyncCallChain<TState>, CancellationToken, Task>> sequence, TState initialState)
            : this(sequence.GetEnumerator(), initialState) 
        { }

        public AsyncCallChain(IEnumerator<Func<IAsyncCallChain<TState>, CancellationToken, Task>> tail, TState initialState)
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

        public Task InvokeNextAsync(CancellationToken cancellationToken)
        {
            var tail = Tail;
            if (tail == null)
                return Task.CompletedTask;
            Position++;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return Task.CompletedTask;
            }
            cancellationToken.ThrowIfCancellationRequested();
            return tail.Current!.Invoke(this, cancellationToken);
        }
    }
    
    public class AsyncCallChain<T, TState> : IAsyncCallChain<TState>
    {
        public IEnumerator<T>? Tail { get; private set; }
        public TState State { get; set; }
        public Func<T, IAsyncCallChain<TState>, CancellationToken, Task> Handler { get; }
        public int Position { get; private set; }

        public AsyncCallChain(IEnumerable<T> sequence, TState initialState, Func<T, IAsyncCallChain<TState>, CancellationToken, Task> handler)
            : this(sequence.GetEnumerator(), initialState, handler) 
        { }

        public AsyncCallChain(IEnumerator<T> tail, TState initialState, Func<T, IAsyncCallChain<TState>, CancellationToken, Task> handler)
        {
            Tail = tail;
            State = initialState;
            Handler = handler;
        }

        public void Dispose()
        {
            var tail = Tail;
            Tail = null;
            tail?.Dispose();
        }

        public Task InvokeNextAsync(CancellationToken cancellationToken)
        {
            var tail = Tail;
            if (tail == null)
                return Task.CompletedTask;
            Position++;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return Task.CompletedTask;
            }
            cancellationToken.ThrowIfCancellationRequested();
            return Handler.Invoke(tail.Current, this, cancellationToken);
        }
    }
}
