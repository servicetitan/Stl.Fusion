using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
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

        public static IServiceCollection Configure<TOptions>(
            this IServiceCollection services,
            Action<IServiceProvider, string?, TOptions> configureOptions)
            where TOptions : class
        {
            if (services == null)
                throw new ArgumentNullException(nameof(services));
            if (configureOptions == null)
                throw new ArgumentNullException(nameof(configureOptions));
            services.AddOptions();
            services.AddSingleton<IConfigureOptions<TOptions>>(
                c => new ConfigureAllNamedOptions<TOptions>(c, configureOptions));
            return services;
        }

        public static ServiceAttributeBasedBuilder AttributeBased(
            this IServiceCollection services)
            => new ServiceAttributeBasedBuilder(services);
        public static ServiceAttributeBasedBuilder AttributeBased(
            this IServiceCollection services,
            Symbol scope)
            => new ServiceAttributeBasedBuilder(services).SetScope(scope);
        public static ServiceAttributeBasedBuilder AttributeBased(
            this IServiceCollection services,
            Symbol scope, Symbol fallbackScope)
            => new ServiceAttributeBasedBuilder(services).SetScope(scope, fallbackScope);
    }
}
