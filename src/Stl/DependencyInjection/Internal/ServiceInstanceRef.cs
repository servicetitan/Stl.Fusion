using System;
using Stl.Comparison;

namespace Stl.DependencyInjection.Internal
{
    public record ServiceInstanceRef(Ref<object?> Instance) : ServiceRef
    {
        public override object? TryResolve(IServiceProvider services)
            => Instance.Target;
    }
}
