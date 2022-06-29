using Stl.Comparison;
using Stl.DependencyInjection.Internal;
using Errors = Stl.DependencyInjection.Internal.Errors;

namespace Stl.DependencyInjection;

public interface IServiceRefProvider
{
    ServiceRef GetServiceRef(object service);
}

public class ServiceRefProvider : IServiceRefProvider
{
    protected ConcurrentDictionary<Type, Type?> ServiceTypeCache { get; } = new();

    public IServiceCollection ServiceCollection { get; init; } = null!;
    public bool AllowServiceInstanceRefs { get; init; }

    public ServiceRefProvider() { }
    public ServiceRefProvider(IServiceCollection serviceCollection, bool allowServiceInstanceRefs = false)
    {
        ServiceCollection = serviceCollection;
        AllowServiceInstanceRefs = allowServiceInstanceRefs;
    }

    public virtual ServiceRef GetServiceRef(object service)
    {
        if (service is IHasServiceRef hsr)
            return hsr.ServiceRef;
        var serviceType = GetServiceType(service.GetType());
        if (serviceType != null)
            return new ServiceTypeRef(service.GetType());
        if (AllowServiceInstanceRefs)
            return new ServiceInstanceRef(Ref.New(service)!);
        throw Errors.NoServiceRef(service.GetType());
    }

    protected Type? GetServiceType(Type implementationType)
        => ServiceTypeCache.GetOrAdd(
            implementationType,
            (implementationType1, self) => self.ServiceCollection
                .Where(d => self.IsMatch(implementationType1, d))
                .Select(d => d.ServiceType)
                .SingleOrDefault(),
            this);

    protected virtual bool IsMatch(Type implementationType, ServiceDescriptor descriptor)
    {
        if (descriptor.Lifetime == ServiceLifetime.Scoped)
            return false;
        if (descriptor.ImplementationType == implementationType)
            return true;
        if (descriptor.ImplementationInstance?.GetType() == implementationType)
            return true;
        if (descriptor.ImplementationFactory != null && descriptor.ServiceType.IsAssignableFrom(implementationType))
            return true;
        return false;
    }
}
