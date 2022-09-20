using Stl.Fusion.Blazor.Internal;

namespace Stl.Fusion.Blazor;

public static class ComputedStateComponent
{
    public static ComputedStateComponentOptions DefaultOptions { get; set; } =
        ComputedStateComponentOptions.SynchronizeComputeState
        | ComputedStateComponentOptions.RecomputeOnParametersSet;
}

public abstract class ComputedStateComponent<TState> : StatefulComponentBase<IComputedState<TState>>
{
    protected ComputedStateComponentOptions Options { get; init; } = ComputedStateComponent.DefaultOptions;

    // State frequently depends on component parameters, so...
    protected override Task OnParametersSetAsync()
    {
        if ((Options & ComputedStateComponentOptions.RecomputeOnParametersSet) == 0)
            return Task.CompletedTask;
        _ = State.Recompute();
        return Task.CompletedTask;
    }

    protected virtual ComputedState<TState>.Options GetStateOptions() => new();

    protected override IComputedState<TState> CreateState()
    {
        // Synchronizes ComputeState call as per:
        // https://github.com/servicetitan/Stl.Fusion/issues/202
        Func<IComputedState<TState>, CancellationToken, Task<TState>> computeState =
            (Options & ComputedStateComponentOptions.SynchronizeComputeState) == 0
                ? UnsynchronizedComputeState
                : DispatcherInfo.FlowsExecutionContext(this)
                    ? SynchronizedComputeState
                    : SynchronizedComputeStateWithExecutionContextFlow;
        return StateFactory.NewComputed(GetStateOptions(), computeState);

        Task<TState> UnsynchronizedComputeState(
            IComputedState<TState> state, CancellationToken cancellationToken)
            => ComputeState(cancellationToken);

        Task<TState> SynchronizedComputeState(
            IComputedState<TState> state, CancellationToken cancellationToken)
        {
            var ts = TaskSource.New<TState>(true);
            _ = InvokeAsync(() => {
                var computeStateTask = ComputeState(cancellationToken);
                return ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
            });
            return ts.Task;
        }

        Task<TState> SynchronizedComputeStateWithExecutionContextFlow(
            IComputedState<TState> state, CancellationToken cancellationToken)
        {
            var ts = TaskSource.New<TState>(true);
            var executionContext = ExecutionContext.Capture();
            if (executionContext == null) {
                // Nothing to restore
                _ = InvokeAsync(() => {
                    var computeStateTask = ComputeState(cancellationToken);
                    return ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                });
            }
            else {
                _ = InvokeAsync(() => {
                    ExecutionContext.Run(executionContext, _ => {
                        var computeStateTask = ComputeState(cancellationToken);
                        ts.TrySetFromTaskAsync(computeStateTask, cancellationToken);
                    }, null);
                    return ts.Task;
                });
            }
            return ts.Task;
        }
    }

    protected abstract Task<TState> ComputeState(CancellationToken cancellationToken);
}
