using System;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Stl.Frozen;
using Stl.Fusion.UI;

namespace Stl.Fusion.Blazor
{
    public abstract class LiveComponentBase<TState> : ComponentBase, IDisposable
    {
        [Inject]
        protected ILiveState<TState> LiveState { get; set; } = null!;
        protected TState State => LiveState.Value;
        protected IUpdateDelayer UpdateDelayer => LiveState.UpdateDelayer;

        public virtual void Dispose() 
            => LiveState.Dispose();

        public void Invalidate(bool updateImmediately = true) 
            => LiveState.Invalidate(updateImmediately);

        protected override void OnInitialized() 
            => LiveState.Updated += OnLiveStateUpdated;

        protected virtual void OnLiveStateUpdated(ILiveState liveState) 
            => StateHasChanged();
    }

    public abstract class LiveComponentBase<TLocalState, TState> : LiveComponentBase<TState>
    {
        protected TLocalState Local {
            get => LiveState.Local;
            set => LiveState.Local = value;
        }

        protected new ILiveState<TLocalState, TState> LiveState {
            get => (ILiveState<TLocalState, TState>) base.LiveState;
            set => base.LiveState = value;
        }

        protected virtual void UpdateLocal(Action<TLocalState> updater)
        {
            var clone = CloneLocal(Local);
            updater.Invoke(clone);
            if (Local is IFrozen f)
                f.Freeze();
            Local = clone;
        }

        protected virtual TLocalState CloneLocal(TLocalState source)
        {
            switch (source) {
            case IFrozen f:
                return (TLocalState) f.CloneToUnfrozen(true);
            default:
                var memberwiseCloneMethod = typeof(object).GetMethod(
                    nameof(MemberwiseClone), 
                    BindingFlags.Instance | BindingFlags.NonPublic);
                return (TLocalState) memberwiseCloneMethod.Invoke(source, Array.Empty<object>());
            }
        }
    }
}
