using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;

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
            => target.AddSingleton(type, source.GetService(type));
        
        public static IServiceCollection CopySingleton<T>(
            this IServiceCollection target, IServiceProvider source)
            where T : class
            => target.AddSingleton(source.GetService<T>());
    }
}
