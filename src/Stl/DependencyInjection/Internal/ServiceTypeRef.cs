using System;

namespace Stl.DependencyInjection.Internal
{
    public record ServiceTypeRef(Type ServiceType) : ServiceRef
    {
        public override object? TryResolve(IServiceProvider services)
            => services.GetService(ServiceType);
    }
}
