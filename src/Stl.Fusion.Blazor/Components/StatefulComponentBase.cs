using Microsoft.AspNetCore.Components;
using Stl.Internal;

namespace Stl.Fusion.Blazor;

public abstract class StatefulComponentBase : FusionComponentBase, IAsyncDisposable, IHandleEvent
{
    private StateEventKind _stateHasChangedTriggers = StateEventKind.Updated;

    [Inject] protected IServiceProvider Services { get; init; } = null!;

    protected IStateFactory StateFactory => Services.StateFactory();
    protected abstract IState UntypedState { get; }
    protected Action<IState, StateEventKind> StateChanged { get; set; }

    protected StateEventKind StateHasChangedTriggers {
        get => _stateHasChangedTriggers;
        set {
            var state = UntypedState;
            if (state == null!) {
                _stateHasChangedTriggers = value;
                return;
            }
            state.RemoveEventHandler(_stateHasChangedTriggers, StateChanged);
            _stateHasChangedTriggers = value;
            state.AddEventHandler(_stateHasChangedTriggers, StateChanged);
        }
    }

    // It's typically more natural for stateful components to recompute State
    // and trigger StateHasChanged only as a result state (re)computation or parameter changes.
    protected bool MustCallStateHasChangedAfterEvent { get; set; } = false;

    protected StatefulComponentBase()
        => StateChanged = (_, _) => this.NotifyStateHasChanged();

    public virtual ValueTask DisposeAsync()
    {
        if (UntypedState is IDisposable d)
            d.Dispose();
        return default;
    }

    Task IHandleEvent.HandleEventAsync(EventCallbackWorkItem callback, object? arg)
    {
        // This code provides support for EnableStateHasChangedCallAfterEvent option
        // See https://github.com/dotnet/aspnetcore/issues/18919#issuecomment-803005864
        var task = callback.InvokeAsync(arg);
        var shouldAwaitTask =
            task.Status != TaskStatus.RanToCompletion &&
            task.Status != TaskStatus.Canceled;
        if (shouldAwaitTask)
            return CallStateHasChangedOnAsyncCompletion(task);

#pragma warning disable VSTHRD103
#pragma warning disable MA0042
        if (MustCallStateHasChangedAfterEvent)
            StateHasChanged();
#pragma warning restore MA0042
#pragma warning restore VSTHRD103
        return Task.CompletedTask;
    }

    private async Task CallStateHasChangedOnAsyncCompletion(Task task)
    {
        try {
            await task;
        }
        catch {
            // Avoiding exception filters for AOT runtime support.
            // Ignore exceptions from task cancelletions, but don't bother issuing a state change.
            if (task.IsCanceled)
                return;
            throw;
        }
#pragma warning disable VSTHRD103
#pragma warning disable MA0042
        if (MustCallStateHasChangedAfterEvent)
            StateHasChanged();
#pragma warning restore MA0042
#pragma warning restore VSTHRD103
    }
}

public abstract class StatefulComponentBase<TState> : StatefulComponentBase
    where TState : class, IState
{
    private TState? _state;

    protected override IState UntypedState => State;

    protected internal TState State {
        get => _state!;
        set {
            if (value == null!)
                throw new ArgumentNullException(nameof(value));
            if (_state != null)
                throw Errors.AlreadyInitialized(nameof(State));
            _state = value;
        }
    }

    protected override void OnInitialized()
    {
        var state = _state ??= CreateState();
        state.AddEventHandler(StateHasChangedTriggers, StateChanged);
    }

    protected virtual TState CreateState()
        => Services.GetRequiredService<TState>();
}
