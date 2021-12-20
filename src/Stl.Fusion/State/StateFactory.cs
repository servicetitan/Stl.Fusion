using Stl.Fusion.Internal;

namespace Stl.Fusion;

public interface IStateFactory : IHasServices
{
    IMutableState<T> NewMutable<T>(MutableState<T>.Options options);

    IComputedState<T> NewComputed<T>(
        ComputedState<T>.Options options,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer);
}

public class StateFactory : IStateFactory
{
    public IServiceProvider Services { get; }

    public StateFactory(IServiceProvider services)
        => Services = services;

    public IMutableState<T> NewMutable<T>(MutableState<T>.Options options)
        => new MutableState<T>(options, Services);

    public IComputedState<T> NewComputed<T>(
        ComputedState<T>.Options options,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        => new FuncComputedState<T>(options, Services, computer);
}
