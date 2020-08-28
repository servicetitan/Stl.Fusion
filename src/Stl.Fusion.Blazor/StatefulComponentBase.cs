using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Stl.Frozen;

namespace Stl.Fusion.Blazor
{
    public abstract class StatefulComponentBase<T> : ComponentBase, IDisposable
    {
        private IState<T> _state = null!;

        [Inject]
        protected IState<T> State {
            get => _state;
            set {
                _state = value;
                OnStateAssigned(value);
            }
        }

        public virtual void Dispose()
        {
            if (State is IDisposable d)
                d.Dispose();
        }

        // Protected methods

        protected override void OnInitialized()
            => State.Updated += state => OnStateUpdated();

        protected virtual void OnStateAssigned(IState<T> state) { }

        protected virtual void OnStateUpdated()
            => InvokeAsync(StateHasChanged);

        // Helpers

        protected static TAny Clone<TAny>(TAny source)
        {
            switch (source) {
            case IFrozen f:
                return (TAny) f.CloneToUnfrozen(true);
            default:
                var memberwiseCloneMethod = typeof(object).GetMethod(
                    nameof(MemberwiseClone),
                    BindingFlags.Instance | BindingFlags.NonPublic);
                return (TAny) memberwiseCloneMethod!.Invoke(source, Array.Empty<object>());
            }
        }
    }
}
