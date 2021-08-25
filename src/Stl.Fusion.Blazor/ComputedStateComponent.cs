using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Blazor
{
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

        protected override IComputedState<TState> CreateState()
            => 0 != (Options & ComputedStateComponentOptions.SynchronizeComputeState)
                ? StateFactory.NewComputed<TState>(ConfigureState,
                    async (_, ct) => {
                        // Synchronizes ComputeState call as per:
                        // https://github.com/servicetitan/Stl.Fusion/issues/202
                        var ts = TaskSource.New<TState>(false);
                        await InvokeAsync(async () => {
                            try {
                                ts.TrySetResult(await ComputeState(ct));
                            }
                            catch (OperationCanceledException) {
                                ts.TrySetCanceled();
                            }
                            catch (Exception e) {
                                ts.TrySetException(e);
                            }
                        });
                        return await ts.Task.ConfigureAwait(false);
                    })
                : StateFactory.NewComputed<TState>(ConfigureState,
                    (_, ct) => ComputeState(ct));

        protected virtual void ConfigureState(ComputedState<TState>.Options options) { }
        protected abstract Task<TState> ComputeState(CancellationToken cancellationToken);
    }
}
