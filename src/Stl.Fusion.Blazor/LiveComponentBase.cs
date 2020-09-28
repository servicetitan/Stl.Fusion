using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Fusion.Blazor
{
    public abstract class LiveComponentBase<T> : StatefulComponentBase<ILiveState<T>>
    {
        protected override void OnInitialized()
        {
            State ??= ServiceProvider.GetService<ILiveState<T>>()
                ?? StateFactory.NewLive<T>(ConfigureState, (_, ct) => ComputeStateAsync(ct), this);
        }

        protected virtual void ConfigureState(LiveState<T>.Options options) { }

        protected virtual Task<T> ComputeStateAsync(CancellationToken cancellationToken)
            // Return the prev. value by default
            => State.AsTask();
    }

    public abstract class LiveComponentBase<T, TLocals> : StatefulComponentBase<ILiveState<T, TLocals>>
    {
        protected IMutableState<TLocals> Locals => State.Locals;

        protected override void OnInitialized()
        {
            State ??= ServiceProvider.GetService<ILiveState<T, TLocals>>()
                ?? StateFactory.NewLive<T, TLocals>(ConfigureState, (_, ct) => ComputeStateAsync(ct), this);
        }

        protected virtual void ConfigureState(LiveState<T, TLocals>.Options options) { }

        protected virtual Task<T> ComputeStateAsync(CancellationToken cancellationToken)
            // Return the prev. value by default
            => State.AsTask();
    }
}
