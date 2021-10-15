namespace Stl.DependencyInjection.Extras.Internal;

public sealed class UnknownServiceActivator : IServiceProvider, IHasServices
{
    public IServiceProvider Services { get; }

    public UnknownServiceActivator(IServiceProvider services)
        => Services = services;

    public object? GetService(Type serviceType)
        => Services.GetOrActivate(serviceType);
}
