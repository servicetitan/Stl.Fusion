using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility
{
    public static class ServiceCollectionEx
    {
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
