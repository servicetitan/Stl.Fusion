using Stl.Fusion.Blazor.Internal;

namespace Stl.Fusion.Blazor;

public static class ComputedStateComponent
{
    public static ComputedStateComponentOptions DefaultOptions { get; set; } =
        ComputedStateComponentOptions.SynchronizeComputeState
        | ComputedStateComponentOptions.RecomputeOnParametersSet;

    public static class DefaultStateOptions
    {
        public static bool MustFlowExecutionContext { get; set; } = false;
    }
}

public abstract class ComputedStateComponent<TState> : StatefulComponentBase<IComputedState<TState>>
{
    public static ComputedState<TState>.Options DefaultStateOptions { get; set; } = new() {
        MustFlowExecutionContext = ComputedStateComponent.DefaultStateOptions.MustFlowExecutionContext
    };

    protected ComputedStateComponentOptions Options { get; init; } = ComputedStateComponent.DefaultOptions;

    // State frequently depends on component parameters, so...
    protected override Task OnParametersSetAsync()
    {
        if ((Options & ComputedStateComponentOptions.RecomputeOnParametersSet) == 0)
            return Task.CompletedTask;
        _ = State.Recompute();
        return Task.CompletedTask;
    }

    protected virtual ComputedState<TState>.Options GetStateOptions()
        => DefaultStateOptions;

    protected override IComputedState<TState> CreateState()
    {
        // Synchronizes ComputeState call as per:
        // https://github.com/servicetitan/Stl.Fusion/issues/202
        ExecutionContext? executionContext;
        Func<IComputedState<TState>, CancellationToken, Task<TState>> computeState;
        if (0 == (Options & ComputedStateComponentOptions.SynchronizeComputeState)) {
            computeState = UnsynchronizedComputeState;
        }
        else {
            if (DispatcherInfo.FlowsExecutionContext(this))
                computeState = SynchronizedComputeState;
            else {
                executionContext = ExecutionContext.Capture();
                if (executionContext == null)
                    computeState = SynchronizedComputeState;
                else
                    computeState = SynchronizedComputeStateWithExecutionContextFlow;
            }
        }
        return StateFactory.NewComputed(GetStateOptions(), computeState);

        async Task<TState> UnsynchronizedComputeState(
            IComputedState<TState> state, CancellationToken cancellationToken)
            => await ComputeState(cancellationToken);

        async Task<TState> SynchronizedComputeState(
            IComputedState<TState> state, CancellationToken cancellationToken)
        {
            var ts = TaskSource.New<TState>(false);
            _ = InvokeAsync(() => {
                var computeStateTask = ComputeState(cancellationToken);
                ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                return Task.CompletedTask;
            });
            return await ts.Task.ConfigureAwait(false);
        }

        async Task<TState> SynchronizedComputeStateWithExecutionContextFlow(
            IComputedState<TState> state, CancellationToken cancellationToken)
        {
            var ts = TaskSource.New<TState>(false);
            _ = InvokeAsync(() => {
                ExecutionContext.Run(executionContext, _ => {
                    var computeStateTask = ComputeState(cancellationToken);
                    ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                }, null);
            });
            return await ts.Task.ConfigureAwait(false);
        }
    }

    protected abstract Task<TState> ComputeState(CancellationToken cancellationToken);
}
