using System;
using System.Collections.Generic;
using System.Reactive;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Internal;

namespace Stl.Async
{
    public interface IAsyncEventSource<out TEvent> : IAsyncEnumerable<TEvent>
    {
        bool IsCompleted { get; }
        bool HasObservers { get; }
        int ObserverCount { get; }
    }

    public class AsyncEventSource<TEvent> : AsyncEventSource<TEvent, Unit>
    {
        public AsyncEventSource(Action<AsyncEventSource<TEvent, Unit>, int, bool>? onObserverCountChanged = null)
            : base(default, onObserverCountChanged)
        { }
    }

    public class AsyncEventSource<TEvent, TTag> : IAsyncDisposable, IAsyncEventSource<TEvent>
    {
        private class State
        {
            public readonly bool IsCompleted;
            public readonly TaskSource<Option<TEvent>> FireSource;
            public readonly TaskSource<Unit> ReadySource;
            public volatile State? NextState;
            public volatile int ObserverCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public State(
                TaskCreationOptions fireTaskCreationOptions,
                TaskCreationOptions readyTaskCreationOptions)
            {
                FireSource = TaskSource.New<Option<TEvent>>(fireTaskCreationOptions);
                ReadySource = TaskSource.New<Unit>(readyTaskCreationOptions);
            }

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public State(State previousState, bool complete)
            {
                IsCompleted = previousState.IsCompleted | complete;
                FireSource = TaskSource.New<Option<TEvent>>(previousState.FireSource.Task.CreationOptions);
                ReadySource = TaskSource.New<Unit>(previousState.ReadySource.Task.CreationOptions);
            }
        }

        private readonly Action<AsyncEventSource<TEvent, TTag>, int, bool>? _onObserverCountChanged;
        private volatile State _state;
        private volatile int _observerCount;
        public TTag Tag { get; }
        public bool IsCompleted => _state.IsCompleted;
        public bool HasObservers => _observerCount != 0;
        public int ObserverCount => _observerCount;

        public AsyncEventSource(
            TTag tag,
            Action<AsyncEventSource<TEvent, TTag>, int, bool>? onObserverCountChanged,
            TaskCreationOptions fireTaskCreationOptions = TaskCreationOptions.RunContinuationsAsynchronously,
            TaskCreationOptions readyTaskCreationOption = TaskCreationOptions.RunContinuationsAsynchronously)
        {
            Tag = tag;
            _state = new State(fireTaskCreationOptions, readyTaskCreationOption);
            _onObserverCountChanged = onObserverCountChanged;
        }

        public override string ToString()
            => $"{GetType().Name}({ObserverCount} observer(s), {nameof(IsCompleted)} = {IsCompleted})";

        public async ValueTask NextAsync(TEvent value, bool failIfCompleted = true)
        {
            var state = SwapState(value!);
            if (state.IsCompleted) {
                if (failIfCompleted)
                    throw Errors.AlreadyCompleted();
                return;
            }
            await WaitAllObserversReadyAsync(state).ConfigureAwait(false);
        }

        // Just a shortcut for NextAsync + CompleteAsync

        public async ValueTask LastAsync(TEvent value, bool failIfCompleted = true)
        {
            await NextAsync(value, failIfCompleted).ConfigureAwait(failIfCompleted);
            await CompleteAsync();
        }

        public async ValueTask<bool> CompleteAsync()
        {
            var state = SwapState(Option.None<TEvent>());
            if (state.IsCompleted)
                return false;
            await WaitAllObserversReadyAsync(state).ConfigureAwait(false);
            return true;
        }

        private Task WaitAllObserversReadyAsync(State state)
        {
            // Please don't modify this code unless you fully understands how it works!
            var spinWait = new SpinWait();
            var readySource = state.ReadySource;
            while (true) {
                // Increment is to make sure no one will trigger readySource (except us)
                if (1 != Interlocked.Increment(ref state.ObserverCount)) {
                    // It wasn't zero, i.e. there were other subscribers
                    if (0 == Interlocked.Decrement(ref state.ObserverCount)) {
                        // We just decremented it to 0, so we have to complete it
                        readySource.TrySetResult(default);
                        return Task.CompletedTask;
                    }
                    // It wasn't 0 after the decrement, so someone else will
                    // complete readySource for sure; we should return its task than.
                    return readySource.Task;
                }
                try {
                    // We know there were no other subscribers when we incremented it,
                    // which might mean that:
                    // 1) Either someone already signaled it
                    if (readySource.Task.IsCompleted)
                        return Task.CompletedTask;
                    // 2) Or there were no subscribers at all
                    if (0 == Interlocked.CompareExchange(ref _observerCount, 0, 0)) {
                        readySource.TrySetResult(default);
                        return Task.CompletedTask;
                    }
                    // 3) Or there were subscribers, but either none of them got into
                    // state.ObserverCount increment-decrement section yet,
                    // or they were exiting the enumerator & couldn't decrement
                    // yet. In both these cases we should just retry w/ spin wait.
                }
                finally {
                    // We should keep increments & decrements balanced
                    Interlocked.Decrement(ref state.ObserverCount);
                }
                spinWait.SpinOnce();
            }
        }

        public async ValueTask DisposeAsync()
            => await CompleteAsync().ConfigureAwait(false);

#pragma warning disable 8425
        public async IAsyncEnumerator<TEvent> GetAsyncEnumerator(
            CancellationToken cancellationToken = default)
#pragma warning restore 8425
        {
            // Please don't modify this code unless you fully understands how it works!
            var observerCount = Interlocked.Increment(ref _observerCount);
            _onObserverCountChanged?.Invoke(this, observerCount, true);
            var isFirst = true;
            var state = _state;
            try {
                while (!state.IsCompleted) {
                    Interlocked.Increment(ref state.ObserverCount);
                    try {
                        // Note that inside this block state.ReadySource state is stable:
                        // it is either completed or not, and no one else may complete
                        // it except us.
                        if (isFirst) {
                            // Is the first state we process, and since we read state
                            // before incrementing state.ObserverCount (we can't do it
                            // differently :) ), there is a chance it was completed
                            // somewhere in between.
                            if (state.ReadySource.Task.IsCompleted) {
                                // It was completed, so all we need to do is to switch
                                // to the next state.
                                continue;
                            }
                            isFirst = false;
                        }
                        var eOption = await state.FireSource
                            .WithCancellation(cancellationToken)
                            .ConfigureAwait(false);
                        if (!eOption.IsSome(out var e))
                            yield break;
                        yield return e;
                    }
                    finally {
                        if (0 == Interlocked.Decrement(ref state.ObserverCount))
                            state.ReadySource.TrySetResult(default);
                        else
                            await state.ReadySource
                                .WithCancellation(cancellationToken)
                                .ConfigureAwait(false);
                        state = state.NextState!;
                    }
                }
            }
            finally {
                observerCount = Interlocked.Decrement(ref _observerCount);
                _onObserverCountChanged?.Invoke(this, observerCount, false);
            }
        }

        private State SwapState(Option<TEvent> eventOpt)
        {
            while (true) {
                var state = _state;
                if (state.IsCompleted)
                    return state;
                var nextState = new State(state, !eventOpt.HasValue);
                if (state == Interlocked.CompareExchange(ref _state, nextState, state)) {
                    Interlocked.Exchange(ref state.NextState, nextState);
                    state.FireSource.SetResult(eventOpt);
                    return state;
                }
            }
        }
    }
}
