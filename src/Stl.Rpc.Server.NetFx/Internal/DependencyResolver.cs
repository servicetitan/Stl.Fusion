using System.Web.Http.Dependencies;

namespace Stl.Rpc.Server.Internal;

public class DependencyResolver(IServiceProvider services) : IDependencyResolver
{
    private readonly IServiceScope? _scope;

    private DependencyResolver(IServiceScope scope) : this(scope.ServiceProvider)
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
        => new DependencyResolver(services.CreateScope());
}
