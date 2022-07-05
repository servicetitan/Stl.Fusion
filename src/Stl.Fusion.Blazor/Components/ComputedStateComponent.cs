namespace Stl.Fusion.Blazor;

public static class ComputedStateComponent
{
    public static ComputedStateComponentOptions DefaultOptions { get; set; } =
        ComputedStateComponentOptions.SynchronizeComputeState
        | ComputedStateComponentOptions.RecomputeOnParametersSet;

    // MAUI's Blazor doesn't flow ExecutionContext into InvokeAsync,
    // so we have to explicitly pass whatever makes sense to pass there.
    // ComputedStateComponent passes Computed.GetCurrent() value to
    // make sure the dependencies are captured when ComputeState runs
    // inside InvokeAsync (w/ SynchronizeComputeState option). 
    public static bool MustPassComputedIntoSynchronizedComputeState { get; set; } = true; 
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

    protected virtual ComputedState<TState>.Options GetStateOptions()
        => new();

    protected override IComputedState<TState> CreateState()
    {
        async Task<TState> SynchronizedComputeState(IComputedState<TState> _, CancellationToken cancellationToken)
        {
            // Synchronizes ComputeState call as per:
            // https://github.com/servicetitan/Stl.Fusion/issues/202
            var ts = TaskSource.New<TState>(false);
            var computed = (IComputed?) null;
            if (ComputedStateComponent.MustPassComputedIntoSynchronizedComputeState)
                computed = Computed.GetCurrent();
            await InvokeAsync(async () => {
                var disposable = computed != null ? Computed.ChangeCurrent(computed) : default;
                try {
                    ts.TrySetResult(await ComputeState(cancellationToken));
                }
                catch (OperationCanceledException) {
                    ts.TrySetCanceled();
                }
                catch (Exception e) {
                    ts.TrySetException(e);
                }
                finally {
                    disposable.Dispose();
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
