using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Blazor
{
    public abstract class StatefulComponentBase : ComponentBase, IDisposable
    {
        [Flags]
        protected enum StateEventHandlers
        {
            OnInvalidated = 1,
            OnUpdating = 2,
            OnUpdated = 4,
            OnAny = OnInvalidated | OnUpdating | OnUpdated,
        }

        private readonly Action<IState> _onStateInvalidatedCached;
        private readonly Action<IState> _onStateUpdatingCached;
        private readonly Action<IState> _onStateUpdatedCached;
        private IState _state = null!;

        [Inject]
        protected IServiceProvider ServiceProvider { get; set; } = null!;
        protected IStateFactory StateFactory => ServiceProvider.GetStateFactory();
        protected StateEventHandlers UsedStateEventHandlers { get; set; } = StateEventHandlers.OnUpdated;
        protected bool IsLoading => _state.Snapshot.UpdateCount != 0;
        protected bool IsUpdating => _state.Snapshot.IsUpdating;
        protected bool IsUpdatePending => _state.Snapshot.Computed.IsInvalidated();

        protected StatefulComponentBase()
        {
            _onStateInvalidatedCached = _ => InvokeAsync(OnStateInvalidated);
            _onStateUpdatingCached = _ => InvokeAsync(OnStateUpdating);
            _onStateUpdatedCached = _ => InvokeAsync(OnStateUpdated);
        }

        public virtual void Dispose()
        {
            var state = _state;
            _state = null!;
            DetachStateEventHandlers(state);
            if (state is IDisposable d)
                d.Dispose();
        }

        // Protected methods

        protected virtual void OnSetState(IState newState)
        {
            var oldState = _state;
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!ReferenceEquals(oldState, null)) {
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
            if ((UsedStateEventHandlers & StateEventHandlers.OnInvalidated) != 0)
                state.Invalidated += _onStateInvalidatedCached;
            if ((UsedStateEventHandlers & StateEventHandlers.OnUpdating) != 0)
                state.Updating += _onStateUpdatingCached;
            if ((UsedStateEventHandlers & StateEventHandlers.OnUpdated) != 0)
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
        private TState? _state = null!;

        protected TState State {
            get => _state!;
            set {
                if (ReferenceEquals(_state, value))
                    return;
                _state = value;
                OnSetState(value);
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
