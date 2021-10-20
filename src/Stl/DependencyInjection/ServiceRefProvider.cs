using Microsoft.Extensions.DependencyInjection;
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
    public class Options
    {
        public IServiceCollection ServiceCollection { get; set; } = null!;
        public bool UseServiceInstanceRefs { get; set; }
    }

    protected IServiceCollection ServiceCollection { get; }
    protected bool UseServiceInstanceRefs { get; }
    protected ConcurrentDictionary<Type, Type?> ServiceTypeCache { get; } = new();

    public ServiceRefProvider(Options options)
    {
        ServiceCollection = options.ServiceCollection
#pragma warning disable MA0015
            ?? throw new ArgumentNullException($"{nameof(options)}.{nameof(ServiceCollection)}");
#pragma warning restore MA0015
        UseServiceInstanceRefs = options.UseServiceInstanceRefs;
    }

    public virtual ServiceRef GetServiceRef(object service)
    {
        if (service is IHasServiceRef hsr)
            return hsr.ServiceRef;
        var serviceType = TryGetServiceType(service.GetType());
        if (serviceType != null)
            return new ServiceTypeRef(service.GetType());
        if (UseServiceInstanceRefs)
            return new ServiceInstanceRef(Ref.New(service)!);
        throw Errors.NoServiceRef(service.GetType());
    }

    protected Type? TryGetServiceType(Type implementationType)
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
