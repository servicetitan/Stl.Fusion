using System;
using System.Collections.Generic;

namespace Stl.Extensibility
{
    public interface ICallChain<TState> : IDisposable
    {
        TState State { get; set; }
        void InvokeNext();
    }
    
    public class CallChain<TState> : ICallChain<TState>
    {
        public IEnumerator<Action<ICallChain<TState>>>? Tail { get; private set; }
        public TState State { get; set; }

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
        public Action<T, ICallChain<TState>> Handler { get; }
        public TState State { get; set; }

        public CallChain(IEnumerable<T> sequence, Action<T, ICallChain<TState>> handler, TState initialState)
            : this(sequence.GetEnumerator(), handler, initialState) 
        { }

        public CallChain(IEnumerator<T> tail, Action<T, ICallChain<TState>> handler, TState initialState)
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

        public void InvokeNext()
        {
            var tail = Tail;
            if (tail == null)
                return;
            if (!tail.MoveNext()) {
                tail.Dispose();
                Tail = null;
                return;
            }
            Handler.Invoke(tail.Current, this);
        }
    }
}