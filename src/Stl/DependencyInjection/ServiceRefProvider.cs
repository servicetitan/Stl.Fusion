using System;
using System.Collections.Concurrent;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;

namespace Stl.DependencyInjection
{
    public interface IServiceRefProvider
    {
        ServiceRef GetServiceRef(object service);
    }

    public class ServiceRefProvider : IServiceRefProvider
    {
        protected IServiceCollection ServiceCollection { get; }
        protected ConcurrentDictionary<Type, Type?> ServiceTypeCache { get; } = new();

        public ServiceRefProvider(IServiceCollection serviceCollection)
            => ServiceCollection = serviceCollection;

        public virtual ServiceRef GetServiceRef(object service)
        {
            if (service is IHasServiceRef hsr)
                return hsr.ServiceRef;
            var serviceType = TryGetServiceType(service.GetType());
            if (serviceType == null)
                throw Errors.NoServiceRef(service.GetType());
            return new ServiceTypeRef(service.GetType());
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
            if (descriptor.ImplementationType == implementationType)
                return true;
            if (descriptor.ImplementationInstance?.GetType() == implementationType)
                return true;
            return false;
        }
    }
}
