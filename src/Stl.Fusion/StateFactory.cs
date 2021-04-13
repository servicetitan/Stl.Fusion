using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public interface IStateFactory : IHasServices
    {
        IMutableState<T> NewMutable<T>(
            MutableState<T>.Options options,
            Option<Result<T>> initialOutput = default);

        ILiveState<T> NewLive<T>(
            LiveState<T>.Options options,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer);
    }

    public class StateFactory : IStateFactory
    {
        public IServiceProvider Services { get; }

        public StateFactory(IServiceProvider services)
            => Services = services;

        public IMutableState<T> NewMutable<T>(
            MutableState<T>.Options options,
            Option<Result<T>> initialOutput = default)
            => new MutableState<T>(options, Services, initialOutput);

        public ILiveState<T> NewLive<T>(
            LiveState<T>.Options options,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer)
            => new FuncLiveState<T>(options, Services, computer);
    }
}
