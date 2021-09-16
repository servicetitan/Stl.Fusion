using System;
using System.Linq;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using Stl.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Text;

namespace Stl.RegisterAttributes
{
    public static class ServiceCollectionExt
    {
        public static bool HasService<TService>(this IServiceCollection services)
            => services.HasService(typeof(TService));
        public static bool HasService(this IServiceCollection services, Type serviceType)
            => services.Any(d => d.ServiceType == serviceType);

        // Options & Settings

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
            services.TryAddSingleton<IConfigureOptions<TOptions>>(
                c => new ConfigureAllNamedOptions<TOptions>(c, configureOptions));
            return services;
        }

        public static IServiceCollection AddSettings<TSettings>(
            this IServiceCollection services,
            string? sectionName = null)
            => services.AddSettings(typeof(TSettings), sectionName);
        public static IServiceCollection AddSettings(
            this IServiceCollection services,
            Type settingsType,
            string? sectionName = null)
        {
            sectionName ??= settingsType.Name.TrimSuffix("Settings", "Cfg", "Config", "Configuration");
            services.TryAddSingleton(settingsType, c => {
                var settings = c.Activate(settingsType);
                var cfg = c.GetRequiredService<IConfiguration>();
                cfg.GetSection(sectionName)?.Bind(settings);
                return settings;
            });
            return services;
        }

        // Attribute-based configuration

        public static RegisterAttributeScanner UseRegisterAttributeScanner(
            this IServiceCollection services, Symbol scope = default)
            => new(services, scope);

        public static IServiceCollection UseRegisterAttributeScanner(
            this IServiceCollection services,
            Action<RegisterAttributeScanner> attributeScannerBuilder)
            => services.UseRegisterAttributeScanner(default, attributeScannerBuilder);

        public static IServiceCollection UseRegisterAttributeScanner(
            this IServiceCollection services, Symbol scope,
            Action<RegisterAttributeScanner> attributeScannerBuilder)
        {
            var builder = services.UseRegisterAttributeScanner(scope);
            attributeScannerBuilder.Invoke(builder);
            return services;
        }
    }
}
