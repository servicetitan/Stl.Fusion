using System;

namespace Stl.DependencyInjection.Internal
{
    public sealed class UnknownServiceActivator : IServiceProvider, IHasServiceProvider
    {
        public IServiceProvider ServiceProvider { get; }

        public UnknownServiceActivator(IServiceProvider serviceProvider)
            => ServiceProvider = serviceProvider;

        public object? GetService(Type serviceType)
            => ServiceProvider.GetOrActivate(serviceType);
    }
}
