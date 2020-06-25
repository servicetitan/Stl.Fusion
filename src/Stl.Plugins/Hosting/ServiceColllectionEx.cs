using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins;
using Stl.Plugins.Hosting.Internal;

namespace Stl.Hosting
{
    public static class ServiceCollectionEx
    {
        // AddHasAutoStartSingleton
        
        public static IServiceCollection AddHasAutoStartSingleton<TService>(
            this IServiceCollection services)
            where TService : class, IHasAutoStart
        {
            services.AddSingleton<TService>();
            services.AddHostedService<HasAutoStartWrapper<TService>>();
            return services;
        }

        public static IServiceCollection AddHasAutoStartSingleton<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> factory)
            where TService : class, IHasAutoStart
        {
            services.AddSingleton(factory);
            services.AddHostedService<HasAutoStartWrapper<TService>>();
            return services;
        }
    }
}
