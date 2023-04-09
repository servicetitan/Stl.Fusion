using System.Web.Http.Dependencies;

namespace Stl.Fusion.Server;

public class DefaultDependencyResolver : IDependencyResolver
{
    private readonly IServiceProvider serviceProvider;
    private readonly IServiceScope? scope;

    public DefaultDependencyResolver(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    private DefaultDependencyResolver(IServiceScope scope)
    {
        this.serviceProvider = scope.ServiceProvider;
        this.scope = scope;
    }

    public object GetService(Type serviceType)
    {
        var service = this.serviceProvider.GetService(serviceType);
        return service!;
    }

    public IEnumerable<object> GetServices(Type serviceType)
    {
        var services = this.serviceProvider.GetServices(serviceType);
        return services!;
    }

    public void Dispose()
    {
        if (serviceProvider is IDisposable disposable)
            disposable.Dispose();

        scope?.Dispose();
    }

    public IDependencyScope BeginScope()
    {
        return new DefaultDependencyResolver(serviceProvider.CreateScope());
    }
}
