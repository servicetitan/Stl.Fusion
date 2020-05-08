using System;
using System.Collections.Generic;
using System.Reactive;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public class AsyncEventSource<TEvent> : IAsyncDisposable, IAsyncEnumerable<TEvent>
    {
        private class State
        {
            public readonly bool IsCompleted;
            public readonly TaskCompletionSource<Option<TEvent>> FireTcs;
            public readonly TaskCompletionSource<Unit> ReadyTcs;
            public readonly TaskCompletionSource<State> NextStateTcs;
            public volatile int ObserverCount;

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

        private static readonly Task<Option<TEvent>> NoEventTask = Task.FromResult(Option<TEvent>.None);
        
        private volatile State _state = new State();
        private volatile int _observerCount = 0;

        public bool IsCompleted => _state.IsCompleted;

        // This method is not thread-safe!
        public async ValueTask PublishAsync(TEvent value)
        {
            var state = SwapState(false).AssertNotCompleted();
            state.FireTcs.SetResult(value!);
            if (state.ObserverCount != 0)
                await state.ReadyTcs.Task.ConfigureAwait(false);
        }

        // This method is not thread-safe!
        public async ValueTask<bool> CompleteAsync()
        {
            var state = SwapState(true);
            if (state.IsCompleted)
                return false;
            state.FireTcs.SetResult(Option<TEvent>.None);
            if (state.ObserverCount != 0)
                await state.ReadyTcs.Task.ConfigureAwait(false);
            return true;
        }

        // This method is not thread-safe!
        public async ValueTask DisposeAsync() 
            => await CompleteAsync().ConfigureAwait(false);

#pragma warning disable 8425
        public async IAsyncEnumerator<TEvent> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
#pragma warning restore 8425
        {
            var state = _state;
            while (true) {
                if (state.IsCompleted)
                    yield break;
                var eOption = await state.FireTcs.Task.ConfigureAwait(false);
                if (!eOption.IsSome(out var e))
                    yield break;
                try {
                    Interlocked.Increment(ref state.ObserverCount);
                    yield return e;
                }
                finally {
                    if (0 == Interlocked.Decrement(ref state.ObserverCount))
                        state.ReadyTcs.TrySetResult(default);
                    else
                        await state.ReadyTcs.Task.ConfigureAwait(false);
                }
                state = await state.NextStateTcs.Task.ConfigureAwait(false);
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
