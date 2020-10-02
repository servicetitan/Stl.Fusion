using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Stl.Internal;

namespace Stl.Fusion.Blazor
{
    public abstract class StatefulComponentBase : ComponentBase, IDisposable
    {

        [Inject]
        protected IServiceProvider ServiceProvider { get; set; } = null!;
        protected IStateFactory StateFactory => ServiceProvider.GetStateFactory();
        protected abstract IState UntypedState { get; }
        protected Action<IState, StateEventKind> StateChanged { get; set; }
        protected StateEventKind StateHasChangedTriggers { get; set; } = StateEventKind.Updated;

        public bool IsLoading => UntypedState == null! || UntypedState.Snapshot.UpdateCount == 0;
        public bool IsUpdating => UntypedState == null! || UntypedState.Snapshot.IsUpdating;
        public bool IsUpdatePending => UntypedState == null! || UntypedState.Snapshot.Computed.IsInvalidated();

        protected StatefulComponentBase()
        {
            StateChanged = (state, eventKind) => InvokeAsync(() => {
                if ((eventKind & StateHasChangedTriggers) != 0)
                    StateHasChanged();
            });
        }

        public virtual void Dispose()
        {
            UntypedState.RemoveEventHandler(StateEventKind.All, StateChanged);
            if (UntypedState is IDisposable d)
                d.Dispose();
        }
    }

    public abstract class StatefulComponentBase<TState> : StatefulComponentBase, IDisposable
        where TState : class, IState
    {
        private TState? _state;

        protected override IState UntypedState => State;

        protected TState State {
            get => _state!;
            set {
                if (value == null)
                    throw new ArgumentNullException(nameof(value));
                if (_state == value)
                    return;
                if (_state != null)
                    throw Errors.AlreadyInitialized(nameof(State));
                _state = value;
            }
        }

        protected override void OnInitialized()
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            State ??= ServiceProvider.GetRequiredService<TState>();
            UntypedState.AddEventHandler(StateEventKind.All, StateChanged);
        }
    }
}
