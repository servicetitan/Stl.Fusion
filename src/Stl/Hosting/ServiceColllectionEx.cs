using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.Async;
using Stl.Hosting.Internal;

namespace Stl.Hosting
{
    public static class ServiceCollectionEx
    {
        // AddAsyncProcessWrapper

        public static IServiceCollection AddAsyncProcess<TService>(
            this IServiceCollection services)
            where TService : class
        {
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }

        // AddAsyncProcessSingleton

        public static IServiceCollection AddAsyncProcessSingleton<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.AddSingleton<TService, TImplementation>();
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }

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

        // TryAddAsyncProcessSingleton

        public static IServiceCollection TryAddAsyncProcessSingleton<TService, TImplementation>(
            this IServiceCollection services)
            where TService : class
            where TImplementation : class, TService
        {
            services.TryAddSingleton<TService, TImplementation>();
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }

        public static IServiceCollection TryAddAsyncProcessSingleton<TService>(
            this IServiceCollection services)
            where TService : class, IAsyncProcess
        {
            services.TryAddSingleton<TService>();
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }

        public static IServiceCollection TryAddAsyncProcessSingleton<TService>(
            this IServiceCollection services, 
            Func<IServiceProvider, TService> factory)
            where TService : class, IAsyncProcess
        {
            services.TryAddSingleton(factory);
            services.AddHostedService<AsyncProcessWrapper<TService>>();
            return services;
        }
    }
}
