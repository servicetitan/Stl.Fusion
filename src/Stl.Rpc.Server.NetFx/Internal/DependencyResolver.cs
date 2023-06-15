using System.Web.Http.Dependencies;

namespace Stl.Rpc.Server.Internal;

public class DependencyResolver : IDependencyResolver
{
    private readonly IServiceProvider _services;
    private readonly IServiceScope? _scope;

    public DependencyResolver(IServiceProvider services)
        => _services = services;

    private DependencyResolver(IServiceScope scope)
    {
        _services = scope.ServiceProvider;
        _scope = scope;
    }

    public object GetService(Type serviceType)
        => _services.GetService(serviceType)!;

    public IEnumerable<object> GetServices(Type serviceType)
        => _services.GetServices(serviceType)!;

    public void Dispose()
    {
        if (_scope is { } dScope)
            dScope.Dispose();
        else if (_services is IDisposable dServices)
            dServices.Dispose();
    }

    public IDependencyScope BeginScope()
        => new DependencyResolver(_services.CreateScope());
}
