using System;
using System.Diagnostics.CodeAnalysis;
using System.Reactive;

namespace Stl.Extensibility
{
    public static class ChainInvocation
    {
        public static ChainInvocation<T, TState> New<T, TState>(
            ReadOnlyMemory<T> tail,
            TState initialState,
            Action<T, ChainInvocation<T, TState>> handler) 
            => new ChainInvocation<T, TState>() {
                Tail = tail,
                Handler = handler,
                State = initialState,
            };

        public static ChainInvocation<T, Unit> New<T>(
            ReadOnlyMemory<T> tail,
            Action<T, ChainInvocation<T, Unit>> handler) 
            => new ChainInvocation<T, Unit>() {
                Tail = tail,
                Handler = handler,
            };

        public static ChainInvocation<T, TState> New<T, TState>(
            T[] tail,
            TState initialState,
            Action<T, ChainInvocation<T, TState>> handler) 
            => new ChainInvocation<T, TState>() {
                Tail = tail,
                Handler = handler,
                State = initialState,
            };

        public static ChainInvocation<T, Unit> New<T>(
            T[] tail,
            Action<T, ChainInvocation<T, Unit>> handler) 
            => new ChainInvocation<T, Unit>() {
                Tail = tail,
                Handler = handler,
            };

        public static TSelf Invoke<TSelf>(this TSelf self)
            where TSelf : ChainInvocationBase
        {
            self.InvokeNext();
            return self;
        }
    }

    public abstract class ChainInvocationBase
    {
        public abstract void InvokeNext();
    }

    public abstract class ChainInvocationBase<TItem> : ChainInvocationBase 
    {
        public ReadOnlyMemory<TItem> Tail { get; set; }
    }

    public abstract class ChainInvocationBase<TItem, TSelf> : ChainInvocationBase<TItem>
        where TSelf : ChainInvocationBase<TItem, TSelf>
    {
        public Action<TItem, TSelf>? Handler { get; set; }

        public override string ToString() 
            => $"{GetType().Name}(Handler={Handler}), Tail=[{Tail.Length} item(s)]";

        public override void InvokeNext()
        {
            if (Tail.Length == 0)
                return;
            var item = Tail.Span[0];
            Tail = Tail.Slice(1); 
            Handler!.Invoke(item, (TSelf) this);
        }
    }

    public class ChainInvocation<TItem, TState> 
        : ChainInvocationBase<TItem, ChainInvocation<TItem, TState>>
    {
        [AllowNull]
        public TState State { get; [return: MaybeNull] set; } = default!;
    }
}
