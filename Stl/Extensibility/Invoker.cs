using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using Stl.Internal;

namespace Stl.Extensibility
{
    public static class Invoker
    {
        public static Invoker<T, TState> New<T, TState>(
            ReadOnlyMemory<T> tail,
            TState initialState,
            Action<T, Invoker<T, TState>> handler) 
            => new Invoker<T, TState>() {
                Tail = tail,
                Handler = handler,
                State = initialState,
            };

        public static Invoker<T, Unit> New<T>(
            ReadOnlyMemory<T> tail,
            Action<T, Invoker<T, Unit>> handler) 
            => new Invoker<T, Unit>() {
                Tail = tail,
                Handler = handler,
            };

        public static Invoker<T, TState> New<T, TState>(
            T[] tail,
            TState initialState,
            Action<T, Invoker<T, TState>> handler) 
            => new Invoker<T, TState>() {
                Tail = tail,
                Handler = handler,
                State = initialState,
            };

        public static Invoker<T, Unit> New<T>(
            T[] tail,
            Action<T, Invoker<T, Unit>> handler) 
            => new Invoker<T, Unit>() {
                Tail = tail,
                Handler = handler,
            };

        public static TSelf Invoke<TSelf>(this TSelf self)
            where TSelf : InvokerBase
        {
            self.Run();
            return self;
        }
    }

    public abstract class InvokerBase
    {
        private InvocationOrder _order;

        public InvocationOrder Order {
            get => _order;
            set {
                AssertNotRunning();
                _order = value;
            }
        }

        public bool IsRunning { get; protected set; }
        public abstract bool HasErrorHandler { get; }

        public abstract void Run();

        protected void AssertNotRunning()
        {
            if (IsRunning)
                throw Errors.InvokerIsAlreadyRunning();
        }
    }

    public abstract class InvokerBase<TItem> : InvokerBase 
    {
        public ReadOnlyMemory<TItem> Tail { get; set; }
    }

    public abstract class InvokerBase<TItem, TSelf> : InvokerBase<TItem>
        where TSelf : InvokerBase<TItem, TSelf>
    {
        private Action<TItem, TSelf>? _handler;
        private Action<Exception, TItem, TSelf>? _errorHandler;

        public Action<TItem, TSelf>? Handler {
            get => _handler;
            set {
                AssertNotRunning();
                _handler = value;
            }
        }

        public Action<Exception, TItem, TSelf>? ErrorHandler {
            get => _errorHandler;
            set {
                AssertNotRunning();
                _errorHandler = value;
            }
        }

        public override bool HasErrorHandler => ErrorHandler != null;

        public override string ToString() 
            => $"{GetType().Name}(Handler={Handler}), Tail=[{Tail.Length} item(s)]";

        public override void Run()
        {
            var oldIsRunning = IsRunning;
            IsRunning = true;
            try {
                if (Order == InvocationOrder.Straight) {
                    while (Tail.Length != 0) {
                        var item = Tail.Span[0];
                        Tail = Tail.Slice(1);
                        InvokeOne(item);
                    }
                }
                else {
                    while (Tail.Length != 0) {
                        var nextTailLength = Tail.Length - 1;
                        var item = Tail.Span[nextTailLength];
                        Tail = Tail.Slice(0, nextTailLength);
                        InvokeOne(item);
                    }
                }
            }
            finally {
                IsRunning = oldIsRunning;
            }
        }

        protected void InvokeOne(TItem item)
        {
            var invoker = (TSelf) this;
            if (ErrorHandler == null)
                Handler!.Invoke(item, invoker);
            else { 
                try {
                    Handler!.Invoke(item, invoker);
                }
                catch (Exception e) {
                    ErrorHandler!.Invoke(e, item, invoker);
                }
            }
        }
    }

    public class Invoker<TItem, TState> 
        : InvokerBase<TItem, Invoker<TItem, TState>>
    {
        [AllowNull]
        public TState State { get; [return: MaybeNull] set; } = default!;
    }
}
