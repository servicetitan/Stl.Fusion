using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Blazor
{
    public abstract class StatefulComponentBase : ComponentBase, IDisposable
    {
        [Flags]
        public enum StateEventHandlers
        {
            Invalidated = 1,
            Updating = 2,
            Updated = 4,
            All = Invalidated | Updating | Updated,
        }

        private readonly Action<IState> _onStateInvalidatedCached;
        private readonly Action<IState> _onStateUpdatingCached;
        private readonly Action<IState> _onStateUpdatedCached;
        private IState? _state;

        [Inject]
        protected IServiceProvider ServiceProvider { get; set; } = null!;
        protected IStateFactory StateFactory => ServiceProvider.GetStateFactory();
        protected StateEventHandlers UsedStateEventHandlers { get; set; } = StateEventHandlers.Updated;

        public bool IsLoading => _state == null || _state.Snapshot.UpdateCount == 0;
        public bool IsUpdating => _state == null || _state.Snapshot.IsUpdating;
        public bool IsUpdatePending => _state == null || _state.Snapshot.Computed.IsInvalidated();

        protected StatefulComponentBase()
        {
            _onStateInvalidatedCached = _ => InvokeAsync(OnStateInvalidated);
            _onStateUpdatingCached = _ => InvokeAsync(OnStateUpdating);
            _onStateUpdatedCached = _ => InvokeAsync(OnStateUpdated);
        }

        public virtual void Dispose()
        {
            var state = _state;
            _state = null;
            if (state != null)
                DetachStateEventHandlers(state);
            if (state is IDisposable d)
                d.Dispose();
        }

        // Protected methods

        protected virtual void OnSetState(IState newState, IState? oldState)
        {
            _state = newState;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (oldState != null) {
                DetachStateEventHandlers(oldState);
                if (oldState is IDisposable d)
                    d.Dispose();
            }
            AttachStateEventHandlers(newState);
        }

        protected virtual void OnStateInvalidated() => StateHasChanged();
        protected virtual void OnStateUpdating() => StateHasChanged();
        protected virtual void OnStateUpdated() => StateHasChanged();

        // Private methods

        private void AttachStateEventHandlers(IState state)
        {
            if ((UsedStateEventHandlers & StateEventHandlers.Invalidated) != 0)
                state.Invalidated += _onStateInvalidatedCached;
            if ((UsedStateEventHandlers & StateEventHandlers.Updating) != 0)
                state.Updating += _onStateUpdatingCached;
            if ((UsedStateEventHandlers & StateEventHandlers.Updated) != 0)
                state.Updated += _onStateUpdatedCached;
        }

        private void DetachStateEventHandlers(IState state)
        {
            state.Invalidated -= _onStateInvalidatedCached;
            state.Updating -= _onStateUpdatingCached;
            state.Updated -= _onStateUpdatedCached;
        }
    }

    public abstract class StatefulComponentBase<TState> : StatefulComponentBase, IDisposable
        where TState : class, IState
    {
        private TState? _state;

        protected TState State {
            get => _state!;
            set {
                var oldState = _state;
                if (ReferenceEquals(oldState, value))
                    return;
                _state = value;
                OnSetState(value, oldState);
            }
        }

        public override void Dispose()
        {
            base.Dispose();
            _state = null!;
        }

        // Protected methods

        protected override void OnInitialized()
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            State ??= ServiceProvider.GetRequiredService<TState>();
        }
    }
}
