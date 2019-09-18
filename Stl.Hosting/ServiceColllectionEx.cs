using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Stl.Plugins;

namespace Stl.Hosting
{
    public static class ServiceCollectionEx
    {
        public static IServiceCollection AddHostedServiceSingleton<TService>(
            this IServiceCollection services)
            where TService : class, IHostedService
        {
            services.AddSingleton<TService>();
            services.AddHostedService<HostedService<TService>>();
            return services;
        }
        
        public static IServiceCollection AddHasAutoStartSingleton<TService>(
            this IServiceCollection services)
            where TService : class, IHasAutoStart
        {
            services.AddSingleton<TService>();
            services.AddHostedService<HasAutoStart<TService>>();
            return services;
        }
    }
}
