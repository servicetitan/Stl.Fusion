using Stl.Fusion.Blazor.Internal;

namespace Stl.Fusion.Blazor;

public static class ComputedStateComponent
{
    public static ComputedStateComponentOptions DefaultOptions { get; set; } =
        ComputedStateComponentOptions.SynchronizeComputeState
        | ComputedStateComponentOptions.RecomputeOnParametersSet;

    public static class DefaultStateOptions
    {
        public static bool PassExecutionContextToUpdateCycle { get; set; } = false;
    }
}

public abstract class ComputedStateComponent<TState> : StatefulComponentBase<IComputedState<TState>>
{
    public static ComputedState<TState>.Options DefaultStateOptions { get; set; } = new() {
        PassExecutionContextToUpdateCycle = ComputedStateComponent.DefaultStateOptions.PassExecutionContextToUpdateCycle
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
        Func<IComputedState<TState>, CancellationToken, Task<TState>> computeState = 
            0 == (Options & ComputedStateComponentOptions.SynchronizeComputeState)
                ? UnsynchronizedComputeState
                : DispatcherInfo.FlowsExecutionContext(this)
                    ? SynchronizedComputeState
                    : SynchronizedComputeStateWithExecutionContextFlow;
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
            });
            return await ts.Task.ConfigureAwait(false);
        }

        async Task<TState> SynchronizedComputeStateWithExecutionContextFlow(
            IComputedState<TState> state, CancellationToken cancellationToken)
        {
            var ts = TaskSource.New<TState>(false);
            var executionContext = ExecutionContext.Capture();
            if (executionContext == null) {
                // Nothing to restore
                _ = InvokeAsync(() => {
                    var computeStateTask = ComputeState(cancellationToken);
                    ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                });
            }
            else {
#if NET5_0_OR_GREATER
                _ = InvokeAsync(() => {
                    ExecutionContext.Restore(executionContext);
                    var computeStateTask = ComputeState(cancellationToken);
                    ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                });
#else
                _ = InvokeAsync(() => {
                    ExecutionContext.Run(executionContext, _ => {
                        var computeStateTask = ComputeState(cancellationToken);
                        ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                    }, null);
                });
#endif
            }
            return await ts.Task.ConfigureAwait(false);
        }
    }

    protected abstract Task<TState> ComputeState(CancellationToken cancellationToken);
}
