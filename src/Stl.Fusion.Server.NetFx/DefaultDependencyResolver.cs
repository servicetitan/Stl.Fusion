using System.Web.Http.Dependencies;

namespace Stl.Fusion.Server;

public class DefaultDependencyResolver(IServiceProvider services) : IDependencyResolver
{
    private readonly IServiceScope? _scope;

    private DefaultDependencyResolver(IServiceScope scope) : this(scope.ServiceProvider)
        => _scope = scope;

    public object GetService(Type serviceType)
        => services.GetService(serviceType)!;

    public IEnumerable<object> GetServices(Type serviceType)
        => services.GetServices(serviceType)!;

    public void Dispose()
    {
        if (_scope is { } dScope)
            dScope.Dispose();
        else if (services is IDisposable dServices)
            dServices.Dispose();
    }

    public IDependencyScope BeginScope()
        => new DefaultDependencyResolver(services.CreateScope());
}
