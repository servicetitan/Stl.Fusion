using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Stl.DependencyInjection.Internal;
using Stl.Internal;
using Stl.Text;

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

        public static IServiceCollection AddService<TImplementation>(
            this IServiceCollection services,
            ServiceAttributeBase? defaultServiceAttribute = null)
            => services.AddService(typeof(TImplementation), defaultServiceAttribute);
        public static IServiceCollection AddService<TImplementation>(
            this IServiceCollection services,
            Symbol scope,
            ServiceAttributeBase? defaultServiceAttribute = null)
            => services.AddService(typeof(TImplementation), scope, defaultServiceAttribute);
        public static IServiceCollection AddService<TImplementation>(
            this IServiceCollection services,
            ServiceAttributeBase[] candidateServiceAttributes,
            ServiceAttributeBase? defaultServiceAttribute = null)
            => services.AddService(typeof(TImplementation), candidateServiceAttributes, defaultServiceAttribute);
        public static IServiceCollection AddService<TImplementation>(
            this IServiceCollection services,
            Symbol scope,
            ServiceAttributeBase[] candidateServiceAttributes,
            ServiceAttributeBase? defaultServiceAttribute = null)
            => services.AddService(typeof(TImplementation), scope, candidateServiceAttributes, defaultServiceAttribute);

        public static IServiceCollection AddService(
            this IServiceCollection services,
            Type implementationType,
            ServiceAttributeBase? defaultServiceAttribute = null)
        {
            var attr = ServiceAttributeBase.Get(implementationType) ?? defaultServiceAttribute;
            if (attr == null)
                throw Errors.NoServiceAttribute(implementationType);
            attr.Register(services, implementationType);
            return services;
        }
        public static IServiceCollection AddService(
            this IServiceCollection services,
            Type implementationType,
            Symbol scope,
            ServiceAttributeBase? defaultServiceAttribute = null)
        {
            var attr = ServiceAttributeBase.Get(implementationType, scope) ?? defaultServiceAttribute;
            if (attr == null)
                throw Errors.NoServiceAttribute(implementationType);
            attr.Register(services, implementationType);
            return services;
        }

        public static IServiceCollection AddService(
            this IServiceCollection services,
            Type implementationType,
            ServiceAttributeBase[] candidateServiceAttributes,
            ServiceAttributeBase? defaultServiceAttribute = null)
        {
            // ReSharper disable once PossibleMultipleEnumeration
            var attr = candidateServiceAttributes.SingleOrDefault(a => a.Scope == ServiceScope.ManualRegistration)
                // ReSharper disable once PossibleMultipleEnumeration
                ?? candidateServiceAttributes.SingleOrDefault(a => a.Scope == ServiceScope.Default)
                ?? defaultServiceAttribute;
            if (attr == null)
                throw Errors.NoServiceAttribute(implementationType);
            attr.Register(services, implementationType);
            return services;
        }
        public static IServiceCollection AddService(
            this IServiceCollection services,
            Type implementationType,
            Symbol scope,
            ServiceAttributeBase[] candidateServiceAttributes,
            ServiceAttributeBase? defaultServiceAttribute = null)
        {
            var attr = candidateServiceAttributes.SingleOrDefault(a => a.Scope == scope.Value)
                ?? defaultServiceAttribute;
            if (attr == null)
                throw Errors.NoServiceAttribute(implementationType);
            attr.Register(services, implementationType);
            return services;
        }

        public static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            params Assembly[] assemblies)
            => services.AddDiscoveredServices("", null!, assemblies);
        public static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            Func<Type, bool> filter,
            params Assembly[] assemblies)
            => services.AddDiscoveredServices("", filter, assemblies);
        public static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            Symbol scope,
            params Assembly[] assemblies)
            => services.AddDiscoveredServices(scope, null!, assemblies);
        public static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            Symbol scope,
            Func<Type, bool> filter,
            params Assembly[] assemblies)
            => services.AddDiscoveredServices(scope, filter, assemblies.SelectMany(a => ServiceInfo.ForAll(a, scope)));

        public static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            IEnumerable<Type> candidates)
            => services.AddDiscoveredServices("", candidates);
        public static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            Symbol scope,
            IEnumerable<Type> candidates)
            => services.AddDiscoveredServices(scope, null, candidates.Select(t => ServiceInfo.For(t, scope)));

        internal static IServiceCollection AddDiscoveredServices(
            this IServiceCollection services,
            Symbol scope,
            Func<Type, bool>? filter,
            IEnumerable<ServiceInfo> candidates)
        {
            filter ??= _ => true;
            foreach (var service in candidates) {
                foreach (var attr in service.Attributes) {
                    var implementationType = service.ImplementationType;
                    if (attr.Scope == scope && filter.Invoke(implementationType))
                        attr.Register(services, implementationType);
                }
            }
            return services;
        }
    }
}
