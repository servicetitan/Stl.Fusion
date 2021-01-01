using System;
using Stl.DependencyInjection.Internal;

namespace Stl.DependencyInjection
{
    [Serializable]
    public abstract record ServiceRef
    {
        public abstract object? TryResolve(IServiceProvider serviceProvider);

        public object Resolve(IServiceProvider serviceProvider)
            => TryResolve(serviceProvider) ?? throw Errors.NoService(this);
    }

    public record ServiceTypeRef(Type Type) : ServiceRef
    {
        public override object? TryResolve(IServiceProvider serviceProvider)
            => serviceProvider.GetService(Type);
    }
}
