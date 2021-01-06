using System;
using Stl.DependencyInjection.Internal;

namespace Stl.DependencyInjection
{
    [Serializable]
    public abstract record ServiceRef
    {
        public abstract object? TryResolve(IServiceProvider services);

        public object Resolve(IServiceProvider services)
            => TryResolve(services) ?? throw Errors.NoService(this);
    }

    public record ServiceTypeRef(Type Type) : ServiceRef
    {
        public override object? TryResolve(IServiceProvider services)
            => services.GetService(Type);
    }
}
