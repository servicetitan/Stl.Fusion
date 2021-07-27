using System;
using Stl.Comparison;

namespace Stl.DependencyInjection.Internal
{
    public record ServiceInstanceRef(Ref<object?> ServiceInstance) : ServiceRef
    {
        public override object? TryResolve(IServiceProvider services)
            => ServiceInstance.Target;
    }
}
