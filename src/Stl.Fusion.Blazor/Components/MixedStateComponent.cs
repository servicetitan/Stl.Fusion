using Stl.Internal;

namespace Stl.Fusion.Blazor;

public abstract class MixedStateComponent<TState, TMutableState> : ComputedStateComponent<TState>
{
    private IMutableState<TMutableState>? _mutableState;

    protected IMutableState<TMutableState> MutableState {
        get => _mutableState!;
        set {
            if (value == null)
                throw new ArgumentNullException(nameof(value));
            if (_mutableState == value)
                return;
            if (_mutableState != null)
                throw Errors.AlreadyInitialized(nameof(MutableState));
            _mutableState = value;
        }
    }

    protected override void OnInitialized()
    {
        // ReSharper disable once ConstantNullCoalescingCondition
        MutableState ??= CreateMutableState();
        MutableState.Updated += (_, _) => _ = State.Recompute();
        base.OnInitialized();
    }

    protected virtual MutableState<TMutableState>.Options GetMutableStateOptions()
        => new();

    protected virtual IMutableState<TMutableState> CreateMutableState()
        => StateFactory.NewMutable(GetMutableStateOptions());
}
