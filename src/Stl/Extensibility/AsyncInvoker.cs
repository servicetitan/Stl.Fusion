using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Extensibility
{
    public static class AsyncInvoker
    {
        public static AsyncInvoker<T, TState> New<T, TState>(
            ReadOnlyMemory<T> tail,
            TState initialState,
            Func<T, AsyncInvoker<T, TState>, CancellationToken, Task> handler,
            InvocationOrder order = InvocationOrder.Straight,
            Action<Exception, T, AsyncInvoker<T, TState>>? errorHandler = null)
            => new() {
                Tail = tail,
                State = initialState,
                Handler = handler,
                Order = order,
                ErrorHandler = errorHandler,
            };

        public static AsyncInvoker<T, Unit> New<T>(
            ReadOnlyMemory<T> tail,
            Func<T, AsyncInvoker<T, Unit>, CancellationToken, Task> handler,
            InvocationOrder order = InvocationOrder.Straight,
            Action<Exception, T, AsyncInvoker<T, Unit>>? errorHandler = null)
            => new() {
                Tail = tail,
                Handler = handler,
                Order = order,
                ErrorHandler = errorHandler,
            };

        public static AsyncInvoker<T, TState> New<T, TState>(
            T[] tail,
            TState initialState,
            Func<T, AsyncInvoker<T, TState>, CancellationToken, Task> handler,
            InvocationOrder order = InvocationOrder.Straight,
            Action<Exception, T, AsyncInvoker<T, TState>>? errorHandler = null)
            => new() {
                Tail = tail,
                State = initialState,
                Handler = handler,
                Order = order,
                ErrorHandler = errorHandler,
            };

        public static AsyncInvoker<T, Unit> New<T>(
            T[] tail,
            Func<T, AsyncInvoker<T, Unit>, CancellationToken, Task> handler,
            InvocationOrder order = InvocationOrder.Straight,
            Action<Exception, T, AsyncInvoker<T, Unit>>? errorHandler = null)
            => new() {
                Tail = tail,
                Handler = handler,
                Order = order,
                ErrorHandler = errorHandler,
            };

        public static async Task<TSelf> RunAsync<TSelf>(
            this TSelf self,
            CancellationToken cancellationToken = default)
            where TSelf : AsyncInvokerBase
        {
            await self.InvokeAsync(cancellationToken).ConfigureAwait(false);
            return self;
        }
    }

    public abstract class AsyncInvokerBase
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

        public abstract Task InvokeAsync(CancellationToken cancellationToken);

        protected void AssertNotRunning()
        {
            if (IsRunning)
                throw Errors.InvokerIsAlreadyRunning();
        }
    }

    public abstract class AsyncInvokerBase<TItem> : AsyncInvokerBase
    {
        public ReadOnlyMemory<TItem> Tail { get; set; }
    }

    public abstract class AsyncInvokerBase<TItem, TSelf> : AsyncInvokerBase<TItem>
        where TSelf : AsyncInvokerBase<TItem, TSelf>
    {
        private Func<TItem, TSelf, CancellationToken, Task>? _handler;
        private Action<Exception, TItem, TSelf>? _errorHandler;

        public Func<TItem, TSelf, CancellationToken, Task>? Handler {
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

        public override async Task InvokeAsync(CancellationToken cancellationToken)
        {
            var oldIsRunning = IsRunning;
            IsRunning = true;
            try {
                if (Order == InvocationOrder.Straight) {
                    while (Tail.Length != 0) {
                        var item = Tail.Span[0];
                        Tail = Tail.Slice(1);
                        await InvokeOne(item, cancellationToken).ConfigureAwait(false);
                    }
                }
                else {
                    while (Tail.Length != 0) {
                        var nextTailLength = Tail.Length - 1;
                        var item = Tail.Span[nextTailLength];
                        Tail = Tail.Slice(0, nextTailLength);
                        await InvokeOne(item, cancellationToken).ConfigureAwait(false);
                    }
                }
            }
            finally {
                IsRunning = oldIsRunning;
            }
        }

        protected async Task InvokeOne(TItem item, CancellationToken cancellationToken)
        {
            var invoker = (TSelf) this;
            if (ErrorHandler == null)
                await Handler!.Invoke(item, invoker, cancellationToken).ConfigureAwait(false);
            else {
                try {
                    await Handler!.Invoke(item, invoker, cancellationToken).ConfigureAwait(false);
                }
                catch (Exception e) {
                    ErrorHandler!.Invoke(e, item, invoker);
                }
            }
        }
    }

    public class AsyncInvoker<TItem, TState>
        : AsyncInvokerBase<TItem, AsyncInvoker<TItem, TState>>
    {
        [AllowNull]
        public TState State { get; [return: MaybeNull] set; } = default!;
    }
}
