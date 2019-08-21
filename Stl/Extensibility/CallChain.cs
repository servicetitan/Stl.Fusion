using System;
using System.Collections.Generic;

namespace Stl.Extensibility
{
    public interface ICallChain<TState> : IDisposable
    {
        TState State { get; set; }
        int Position { get; }
        void InvokeNext();
    }
    
    public class CallChain<TState> : ICallChain<TState>
    {
        public IEnumerator<Action<ICallChain<TState>>>? Tail { get; private set; }
        public TState State { get; set; }
        public int Position { get; private set; }

        public CallChain(IEnumerable<Action<ICallChain<TState>>> sequence, TState initialState)
            : this(sequence.GetEnumerator(), initialState) 
        { }

        public CallChain(IEnumerator<Action<ICallChain<TState>>> tail, TState initialState)
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

        public void InvokeNext()
        {
            var tail = Tail;
            if (tail == null)
                return;
            Position++;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return;
            }
            tail.Current!.Invoke(this);
        }
    }
    
    public class CallChain<T, TState> : ICallChain<TState>
    {
        public IEnumerator<T>? Tail { get; private set; }
        public TState State { get; set; }
        public Action<T, ICallChain<TState>> Handler { get; }
        public int Position { get; private set; }

        public CallChain(IEnumerable<T> sequence, TState initialState, Action<T, ICallChain<TState>> handler)
            : this(sequence.GetEnumerator(), initialState, handler) 
        { }

        public CallChain(IEnumerator<T> tail, TState initialState, Action<T, ICallChain<TState>> handler)
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

        public void InvokeNext()
        {
            var tail = Tail;
            if (tail == null)
                return;
            Position++;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return;
            }
            Handler.Invoke(tail.Current, this);
        }
    }
}
