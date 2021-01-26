using System;
using Stl.Internal;

namespace Stl.DependencyInjection.Internal
{
    public record ServiceInstanceRef(RefBox<object?> ServiceInstance) : ServiceRef
    {
        public override object? TryResolve(IServiceProvider services)
            => ServiceInstance.Target;
    }
}
