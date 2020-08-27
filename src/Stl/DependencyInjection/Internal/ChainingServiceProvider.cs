using System;

namespace Stl.DependencyInjection.Internal
{
    public sealed class ChainingServiceProvider : IServiceProvider
    {
        public IServiceProvider PrimaryServiceProvider { get; }
        public IServiceProvider SecondaryServiceProvider { get; }

        public ChainingServiceProvider(
            IServiceProvider primaryServiceProvider,
            IServiceProvider secondaryServiceProvider)
        {
            PrimaryServiceProvider = primaryServiceProvider;
            SecondaryServiceProvider = secondaryServiceProvider;
        }

        public object? GetService(Type serviceType)
            => PrimaryServiceProvider.GetService(serviceType) ?? SecondaryServiceProvider.GetService(serviceType);
    }
}
