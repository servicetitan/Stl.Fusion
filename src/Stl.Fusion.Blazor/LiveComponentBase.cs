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
                ?? StateFactory.NewLive<T>(ConfigureState, ComputeStateAsync, this);
        }

        protected virtual void ConfigureState(LiveState<T>.Options options) { }

        protected virtual Task<T> ComputeStateAsync(ILiveState<T> state, CancellationToken cancellationToken)
            // No updates by default
            => state.AsTask();
    }

    public abstract class LiveComponentBase<T, TOwn> : StatefulComponentBase<ILiveState<T, TOwn>>
    {
        protected IMutableState<TOwn> OwnState => State.OwnState;

        protected override void OnInitialized()
        {
            State ??= ServiceProvider.GetService<ILiveState<T, TOwn>>()
                ?? StateFactory.NewLive<T, TOwn>(ConfigureState, ComputeStateAsync, this);
        }

        protected virtual void ConfigureState(LiveState<T, TOwn>.Options options) { }

        protected virtual Task<T> ComputeStateAsync(ILiveState<T, TOwn> state, CancellationToken cancellationToken)
            // No updates by default
            => state.AsTask();
    }
}
