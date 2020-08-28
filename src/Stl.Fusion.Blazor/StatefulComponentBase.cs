using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Blazor
{
    public abstract class StatefulComponentBase<TState> : ComponentBase, IDisposable
        where TState : class, IState
    {
        private readonly Action<IState> _onStateUpdatedCached;
        private TState? _state = null!;

        [Inject]
        protected IServiceProvider ServiceProvider { get; set; } = null!;
        protected IStateFactory StateFactory => ServiceProvider.GetStateFactory();

        protected virtual TState State {
            get => _state!;
            set {
                var oldState = _state;
                if (!ReferenceEquals(oldState, null)) {
                    oldState.Updated -= _onStateUpdatedCached;
                    if (oldState is IDisposable d)
                        d.Dispose();
                }
                _state = value;
                _state.Updated += _onStateUpdatedCached;
            }
        }

        protected StatefulComponentBase()
            => _onStateUpdatedCached = _ => OnStateUpdated();

        public virtual void Dispose()
        {
            if (State is IDisposable d)
                d.Dispose();
        }

        // Protected methods

        protected override void OnInitialized()
        {
            // ReSharper disable once ConstantNullCoalescingCondition
            State ??= ServiceProvider.GetRequiredService<TState>();
        }

        protected virtual void OnStateUpdated()
            => InvokeAsync(StateHasChanged);
    }
}
