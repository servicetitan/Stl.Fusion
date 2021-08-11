using System;
using Stl.DependencyInjection.Extras.Internal;

namespace Stl.DependencyInjection.Extras
{
    public static class ServiceProviderEx
    {
        // With/As helpers

        public static IServiceProvider AsServiceProvider(this Func<Type, object?> serviceProvider)
            => (FuncServiceProvider) serviceProvider;

        public static IServiceProvider WithSecondary(this IServiceProvider primary, IServiceProvider secondary)
            => new ChainingServiceProvider(primary, secondary);
        public static IServiceProvider WithSecondary(this IServiceProvider primary, Func<Type, object?> secondary)
            => new ChainingServiceProvider(primary, (FuncServiceProvider) secondary);

        public static IServiceProvider WithPrimary(this IServiceProvider secondary, IServiceProvider primary)
            => new ChainingServiceProvider(primary, secondary);
        public static IServiceProvider WithPrimary(this IServiceProvider secondary, Func<Type, object?> primary)
            => new ChainingServiceProvider((FuncServiceProvider) primary, secondary);

        public static IServiceProvider WithUnknownServiceActivator(this IServiceProvider services)
            => new UnknownServiceActivator(services);
    }
}
