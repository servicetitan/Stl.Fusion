using Stl.Fusion.Internal;

namespace Stl.Fusion;

public interface IStateFactory : IHasServices
{
    IMutableState<T> NewMutable<T>(MutableState<T>.Options settings);

    IComputedState<T> NewComputed<T>(
        ComputedState<T>.Options settings,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer);
}

public class StateFactory(IServiceProvider services) : IStateFactory
{
    public IServiceProvider Services { get; } = services;

    public IMutableState<T> NewMutable<T>(MutableState<T>.Options settings)
        => new MutableState<T>(settings, Services);

    public IComputedState<T> NewComputed<T>(
        ComputedState<T>.Options settings,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        => new FuncComputedState<T>(settings, Services, computer);
}
