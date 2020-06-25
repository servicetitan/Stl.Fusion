using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Stl.Extensibility
{
    public static class ServiceCollectionEx
    {
        public static bool HasService<TService>(this IServiceCollection services) 
            => services.HasService(typeof(TService));
        public static bool HasService(this IServiceCollection services, Type serviceType) 
            => services.Any(d => d.ServiceType == serviceType);
        
        public static IServiceCollection CopySingleton(
            this IServiceCollection target,
            IServiceProvider source, Type type)
            => target.AddSingleton(type, source.GetRequiredService(type));
        
        public static IServiceCollection CopySingleton<T>(
            this IServiceCollection target, IServiceProvider source)
            where T : class
            => target.AddSingleton(source.GetRequiredService<T>());

        public static IServiceCollection TryCopySingleton<T>(
            this IServiceCollection target, IServiceProvider source)
            where T : class
        {
            var service = source.GetService<T>();
            if (service != null)
                target.TryAddSingleton(service);
            return target;
        }

        public static IServiceCollection AddFactorySingleton<TService, TFactory>(
            this IServiceCollection services)
            where TService : class
            where TFactory : class, IFactory<TService>
            => services
                .AddSingleton<IFactory<TService>, TFactory>()
                .AddSingleton(c => c.GetRequiredService<IFactory<TService>>().Create());

        public static IServiceCollection AddFactoryScoped<TService, TFactory>(
            this IServiceCollection services)
            where TService : class
            where TFactory : class, IFactory<TService>
            => services
                .AddScoped<IFactory<TService>, TFactory>()
                .AddScoped(c => c.GetRequiredService<IFactory<TService>>().Create());
    }
}
