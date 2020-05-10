using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public class AsyncEventSource<TEvent> : AsyncEventSource<TEvent, Unit>
    {
        public AsyncEventSource(Action<AsyncEventSource<TEvent, Unit>, int, bool>? onObserverCountChanged = null) 
            : base(default, onObserverCountChanged) 
        { }
    }

    public class AsyncEventSource<TEvent, TTag> : IAsyncDisposable, IAsyncEnumerable<TEvent>
    {
        private class State
        {
            public readonly bool IsCompleted;
            public readonly TaskCompletionSource<Option<TEvent>> FireTcs;
            public readonly TaskCompletionSource<Unit> ReadyTcs;
            public readonly TaskCompletionSource<State> NextStateTcs;
            public volatile int ObservingObserverCount;

            public State()
            {
                FireTcs = new TaskCompletionSource<Option<TEvent>>(
                    TaskCreationOptions.RunContinuationsAsynchronously);
                ReadyTcs = new TaskCompletionSource<Unit>();
                NextStateTcs = new TaskCompletionSource<State>();
            }

            public State(State previousState, bool complete) : this() 
                => IsCompleted = previousState.IsCompleted | complete;

            public State AssertNotCompleted() 
                => IsCompleted ? throw Errors.AlreadyCompleted() : this;
        }

        private readonly Action<AsyncEventSource<TEvent, TTag>, int, bool>? _onObserverCountChanged; 
        private volatile State _state = new State();
        private volatile int _observerCount;
        public TTag Tag { get; }
        public bool IsCompleted => _state.IsCompleted;
        public bool HasObservers => _observerCount != 0;
        public int ObserverCount => _observerCount;

        public AsyncEventSource(
            TTag tag, 
            Action<AsyncEventSource<TEvent, TTag>, int, bool>? onObserverCountChanged)
        {
            Tag = tag;
            _onObserverCountChanged = onObserverCountChanged;
        }

        public override string ToString() 
            => $"{GetType().Name}({ObserverCount} observer(s), {nameof(IsCompleted)} = {IsCompleted})";

        public async ValueTask NextAsync(TEvent value, bool failIfCompleted = true)
        {
            var state = SwapState(false);
            if (failIfCompleted)
                state.AssertNotCompleted();
            state.FireTcs.SetResult(value!);
            if (state.ObservingObserverCount != 0) {
                await state.ReadyTcs.Task.ConfigureAwait(false);
            }
        }

        // Just a shortcut for NextAsync + CompleteAsync
        public async ValueTask LastAsync(TEvent value, bool failIfCompleted = true)
        {
            await NextAsync(value, failIfCompleted).ConfigureAwait(failIfCompleted);
            await CompleteAsync();
        }

        public async ValueTask<bool> CompleteAsync()
        {
            var state = SwapState(true);
            if (state.IsCompleted)
                return false;
            state.FireTcs.SetResult(Option<TEvent>.None);
            if (state.ObservingObserverCount != 0)
                await state.ReadyTcs.Task.ConfigureAwait(false);
            return true;
        }

        public async ValueTask DisposeAsync() 
            => await CompleteAsync().ConfigureAwait(false);

#pragma warning disable 8425
        public async IAsyncEnumerator<TEvent> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
#pragma warning restore 8425
        {
            var state = _state;
            var observerCount = Interlocked.Increment(ref _observerCount);
            _onObserverCountChanged?.Invoke(this, observerCount, true);
            try {
                while (true) {
                    if (state.IsCompleted)
                        yield break;
                    cancellationToken.ThrowIfCancellationRequested();
                    var eOption = await state.FireTcs.Task.ConfigureAwait(false);
                    if (!eOption.IsSome(out var e))
                        yield break;
                    try {
                        cancellationToken.ThrowIfCancellationRequested();
                        Interlocked.Increment(ref state.ObservingObserverCount);
                        yield return e;
                    }
                    finally {
                        if (0 == Interlocked.Decrement(ref state.ObservingObserverCount))
                            state.ReadyTcs.TrySetResult(default);
                        else
                            await state.ReadyTcs.Task.ConfigureAwait(false);
                    }
                    cancellationToken.ThrowIfCancellationRequested();
                    state = await state.NextStateTcs.Task.ConfigureAwait(false);
                }
            }
            finally {
                observerCount = Interlocked.Decrement(ref _observerCount);
                _onObserverCountChanged?.Invoke(this, observerCount, false);
            }
        }

        private State SwapState(bool complete)
        {
            while (true) {
                var state = _state;
                var nextState = new State(state, complete);
                if (state == Interlocked.CompareExchange(ref _state, nextState, state)) {
                    state.NextStateTcs.SetResult(nextState);
                    return state;
                }
            }
        }
    }
}
