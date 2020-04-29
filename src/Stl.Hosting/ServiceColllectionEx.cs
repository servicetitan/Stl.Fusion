using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stl.Async;
using Stl.Hosting.HostedServices;
using Stl.Plugins;

namespace Stl.Hosting
{
    public static class ServiceCollectionEx
    {
        // AddHostedServiceSingleton

        public static IServiceCollection AddHostedServiceSingleton<TService>(
            this IServiceCollection services)
            where TService : class, IHostedService
        {
            services.AddSingleton<TService>();
            services.AddHostedService<HostedServiceWrapper<TService>>();
            return services;
        }

        public static IServiceCollection AddHostedServiceSingleton<TService>(
            this IServiceCollection services,
            Func<IServiceProvider, TService> factory)
            where TService : class, IHostedService
        {
            services.AddSingleton(factory);
            services.AddHostedService<HostedServiceWrapper<TService>>();
            return services;
        }

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

        // AddAsyncProcessSingleton

        public static IServiceCollection AddAsyncProcessSingleton<TService>(
            this IServiceCollection services)
            where TService : class, IAsyncProcess
        {
            services.AddSingleton<TService>();
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }

        public static IServiceCollection AddAsyncProcessSingleton<TService>(
            this IServiceCollection services, 
            Func<IServiceProvider, TService> factory)
            where TService : class, IAsyncProcess
        {
            services.AddSingleton(factory);
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }
    }
}
