using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility 
{
    public static class ServiceProviderEx
    {
        public static IServiceProvider Unavailable { get; } =
            new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection());

        public static bool IsAvailable(this IServiceProvider serviceProvider)
            => serviceProvider != Unavailable;
    }
}
