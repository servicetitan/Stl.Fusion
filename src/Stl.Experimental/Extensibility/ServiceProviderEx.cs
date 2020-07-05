using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.Extensibility 
{
    public static class ServiceProviderEx
    {
        // Get(Required)SpecialService

        public static TService GetSpecialService<TService, TFor>(this IServiceProvider services)
            where TService : class
        {
            var special = services.GetService<Special<TService, TFor>>();
            return special?.Service ?? services.GetService<TService>(); 
        }

        public static TService GetSpecialService<TService>(this IServiceProvider services, Type forType)
            where TService : class
        {
            var specialType = typeof(Special<,>).MakeGenericType(typeof(TService), forType);
            var special = services.GetService(specialType) as ISpecial<TService>;
            return special?.Service ?? services.GetService<TService>();
        }

        public static object GetSpecialService(this IServiceProvider services, Type serviceType, Type forType)
        {
            var specialType = typeof(Special<,>).MakeGenericType(serviceType, forType);
            var special = (ISpecial) services.GetService(specialType);
            return special?.Service ?? services.GetService(serviceType);
        }

        public static TService GetRequiredSpecialService<TService, TFor>(this IServiceProvider services)
            where TService : class
        {
            var special = services.GetService<Special<TService, TFor>>();
            return special?.Service ?? services.GetRequiredService<TService>(); 
        }

        public static TService GetRequiredSpecialService<TService>(this IServiceProvider services, Type forType)
            where TService : class
        {
            var specialType = typeof(Special<,>).MakeGenericType(typeof(TService), forType);
            var special = services.GetService(specialType) as ISpecial<TService>;
            return special?.Service ?? services.GetRequiredService<TService>();
        }

        public static object GetRequiredSpecialService(this IServiceProvider services, Type serviceType, Type forType)
        {
            var specialType = typeof(Special<,>).MakeGenericType(serviceType, forType);
            var special = (ISpecial) services.GetService(specialType);
            return special?.Service ?? services.GetRequiredService(serviceType);
        }
    }
}
