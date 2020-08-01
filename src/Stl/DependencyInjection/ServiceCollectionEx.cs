using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection.Internal;

namespace Stl.DependencyInjection
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

        // [Service]-based automatic service discovery

        public static IServiceCollection AddServices(
            this IServiceCollection services,
            params Assembly[] assemblies)
            => services.AddServices("", null!, assemblies);
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            Func<Type, bool> filter,
            params Assembly[] assemblies)
            => services.AddServices("", filter, assemblies);
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            string scope,
            params Assembly[] assemblies)
            => services.AddServices(scope, null!, assemblies);
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            string scope,
            Func<Type, bool> filter,
            params Assembly[] assemblies)
            => services.AddServices(scope, filter, assemblies.SelectMany(a => ServiceInfo.ForAll(a, scope)));

        public static IServiceCollection AddServices(
            this IServiceCollection services,
            IEnumerable<Type> candidates)
            => services.AddServices("", candidates);
        public static IServiceCollection AddServices(
            this IServiceCollection services,
            string scope,
            IEnumerable<Type> candidates)
            => services.AddServices(scope, null, candidates.Select(t => ServiceInfo.For(t, scope)));

        internal static IServiceCollection AddServices(
            this IServiceCollection services,
            string scope,
            Func<Type, bool>? filter,
            IEnumerable<ServiceInfo?> candidates)
        {
            filter ??= _ => true;
            foreach (var service in candidates) {
                if (service == null)
                    continue;
                foreach (var attr in service.Attributes) {
                    var implementationType = service.ImplementationType;
                    if (attr.Scope == scope && filter.Invoke(implementationType))
                        attr.TryRegister(services, implementationType);
                }
            }
            return services;
        }
    }
}
