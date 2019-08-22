using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Extensibility
{
    public static class AsyncChainInvocation
    {
        public static AsyncChainInvocation<T, TState> New<T, TState>(
            ReadOnlyMemory<T> tail,
            TState initialState,
            Func<T, AsyncChainInvocation<T, TState>, CancellationToken, Task> handler) 
            => new AsyncChainInvocation<T, TState>() {
                Tail = tail,
                Handler = handler,
                State = initialState,
            };

        public static AsyncChainInvocation<T, Unit> New<T>(
            ReadOnlyMemory<T> tail,
            Func<T, AsyncChainInvocation<T, Unit>, CancellationToken, Task> handler) 
            => new AsyncChainInvocation<T, Unit>() {
                Tail = tail,
                Handler = handler,
            };

        public static AsyncChainInvocation<T, TState> New<T, TState>(
            T[] tail,
            TState initialState,
            Func<T, AsyncChainInvocation<T, TState>, CancellationToken, Task> handler) 
            => new AsyncChainInvocation<T, TState>() {
                Tail = tail,
                Handler = handler,
                State = initialState,
            };

        public static AsyncChainInvocation<T, Unit> New<T>(
            T[] tail,
            Func<T, AsyncChainInvocation<T, Unit>, CancellationToken, Task> handler) 
            => new AsyncChainInvocation<T, Unit>() {
                Tail = tail,
                Handler = handler,
            };

        public static async Task<TSelf> InvokeAsync<TSelf>(
            this TSelf self, 
            CancellationToken cancellationToken = default)
            where TSelf : AsyncChainInvocationBase
        {
            await self.InvokeNextAsync(cancellationToken);
            return self;
        }
    }

    public abstract class AsyncChainInvocationBase
    {
        public abstract Task InvokeNextAsync(CancellationToken cancellationToken);
    }

    public abstract class AsyncChainInvocationBase<TItem> : AsyncChainInvocationBase
    {
        public ReadOnlyMemory<TItem> Tail { get; set; }
    }

    public abstract class AsyncChainInvocationBase<TItem, TSelf> : AsyncChainInvocationBase<TItem>
        where TSelf : AsyncChainInvocationBase<TItem, TSelf>
    {
        public Func<TItem, TSelf, CancellationToken, Task>? Handler { get; set; }

        public override string ToString() 
            => $"{GetType().Name}(Handler={Handler}), Tail=[{Tail.Length} item(s)]";

        public override async Task InvokeNextAsync(CancellationToken cancellationToken)
        {
            // It's "while" (not "if") solely to make sure that if Handler
            // somehow doesn't call InvokeNextAsync, it automatically happens
            // later here (and in this case w/o recursion). I decided it's 
            // safer to always "exhaust" the Tail here vs increasing a chance
            // of investigating an issue that's really hard to find.
            while (Tail.Length != 0) {
                var item = Tail.Span[0];
                Tail = Tail.Slice(1); 
                await Handler!.Invoke(item, (TSelf) this, cancellationToken);
            }
        }
    }

    public class AsyncChainInvocation<TItem, TState> 
        : AsyncChainInvocationBase<TItem, AsyncChainInvocation<TItem, TState>>
    {
        [AllowNull]
        public TState State { get; [return: MaybeNull] set; } = default!;
    }
}
