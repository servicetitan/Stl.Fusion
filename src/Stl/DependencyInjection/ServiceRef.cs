using Stl.DependencyInjection.Internal;

namespace Stl.DependencyInjection;

public abstract record ServiceRef
{
    public abstract object? TryResolve(IServiceProvider services);

    public object Resolve(IServiceProvider services)
        => TryResolve(services) ?? throw Errors.NoService(this);
}
