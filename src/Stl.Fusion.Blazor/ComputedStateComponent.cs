namespace Stl.Fusion.Blazor;

public abstract class ComputedStateComponent<TState> : StatefulComponentBase<IComputedState<TState>>
{
    protected ComputedStateComponentOptions Options { get; set; } =
        ComputedStateComponentOptions.SynchronizeComputeState
        | ComputedStateComponentOptions.RecomputeOnParametersSet;

    // State frequently depends on component parameters, so...
    protected override Task OnParametersSetAsync()
    {
        if (0 == (Options & ComputedStateComponentOptions.RecomputeOnParametersSet))
            return Task.CompletedTask;
        State.Recompute();
        return Task.CompletedTask;
    }

    protected virtual ComputedState<TState>.Options GetStateOptions()
        => new();

    protected override IComputedState<TState> CreateState()
    {
        async Task<TState> SynchronizedComputeState(IComputedState<TState> _, CancellationToken cancellationToken)
        {
            // Synchronizes ComputeState call as per:
            // https://github.com/servicetitan/Stl.Fusion/issues/202
            var ts = TaskSource.New<TState>(false);
            await InvokeAsync(async () => {
                try {
                    ts.TrySetResult(await ComputeState(cancellationToken));
                }
                catch (OperationCanceledException) {
                    ts.TrySetCanceled();
                }
                catch (Exception e) {
                    ts.TrySetException(e);
                }
            });
            return await ts.Task.ConfigureAwait(false);
        }

        return StateFactory.NewComputed(GetStateOptions(),
            0 != (Options & ComputedStateComponentOptions.SynchronizeComputeState)
            ? SynchronizedComputeState
            : (_, ct) => ComputeState(ct));
    }

    protected abstract Task<TState> ComputeState(CancellationToken cancellationToken);
}
