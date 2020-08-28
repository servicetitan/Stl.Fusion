using System;
using System.Threading;
using System.Threading.Tasks;
using Stl.DependencyInjection;
using Stl.Fusion.Internal;

namespace Stl.Fusion
{
    public interface IStateFactory : IHasServiceProvider
    {
        IMutableState<T> NewMutable<T>(
            MutableState<T>.Options options,
            Option<Result<T>> initialOutput = default,
            object? argument = null);

        IComputedState<T> NewComputed<T>(
            ComputedState<T>.Options options,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer,
            object? argument = null);

        ILiveState<T> NewLive<T>(
            LiveState<T>.Options options,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer,
            object? argument = null);

        ILiveState<T, TOwn> NewLive<T, TOwn>(
            LiveState<T, TOwn>.Options options,
            Func<ILiveState<T, TOwn>, CancellationToken, Task<T>> computer,
            object? argument = null);
    }

    public class StateFactory : IStateFactory
    {
        public IServiceProvider ServiceProvider { get; }

        public StateFactory(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider;

        public IMutableState<T> NewMutable<T>(
            MutableState<T>.Options options,
            Option<Result<T>> initialOutput = default,
            object? argument = null)
            => new MutableState<T>(options, ServiceProvider, initialOutput, argument);

        public IComputedState<T> NewComputed<T>(
            ComputedState<T>.Options options,
            Func<IComputedState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
            => new FuncComputedState<T>(options, ServiceProvider, computer, argument);

        public ILiveState<T> NewLive<T>(
            LiveState<T>.Options options,
            Func<ILiveState<T>, CancellationToken, Task<T>> computer,
            object? argument = null)
            => new FuncLiveState<T>(options, ServiceProvider, computer, argument);

        public ILiveState<T, TOwn> NewLive<T, TOwn>(
            LiveState<T, TOwn>.Options options,
            Func<ILiveState<T, TOwn>, CancellationToken, Task<T>> computer,
            object? argument = null)
            => new FuncLiveState<T, TOwn>(options, ServiceProvider, computer, argument);
    }
}
