using Stl.Fusion.Internal;

namespace Stl.Fusion;

public interface IStateFactory : IHasServices
{
    IMutableState<T> NewMutable<T>(
        MutableState<T>.Options options,
        Option<Result<T>> initialOutput = default);

    IComputedState<T> NewComputed<T>(
        ComputedState<T>.Options options,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer);
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

    public IComputedState<T> NewComputed<T>(
        ComputedState<T>.Options options,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
        => new FuncComputedState<T>(options, Services, computer);
}
