using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Stl.Internal;

namespace Stl.Extensibility 
{
    public static class ServiceProviderEx
    {
        public static IServiceProvider Empty { get; } =
            new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection());

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

        // Activate / TryActivate

        public static T Activate<T>(this IServiceProvider services)
            where T : class
            => (T) services.Activate(typeof(T));

        public static T Activate<T>(this IServiceProvider services, ConstructorInfo constructorInfo)
            where T : class
            => (T) services.Activate(typeof(T), constructorInfo);

        // The current impl. is super slow; use with caution.
        public static object Activate(this IServiceProvider services, Type type)
        {
            var ctors = type.GetConstructors()
                .OrderByDescending(ci => ci.GetParameters().Length)
                .ToList();

            var primaryCtor = ctors
                .SingleOrDefault(ci => ci.GetCustomAttribute<ServiceConstructorAttribute>() != null);
            if (primaryCtor != null)
                return services.Activate(type, primaryCtor);

            foreach (var ctor in ctors)
                if (services.TryActivate(type, ctor, out var result))
                    return result;
            throw Errors.CannotActivate(type);
        }

        public static object Activate(this IServiceProvider services, 
            Type type, ConstructorInfo constructorInfo)
        {
            if (services.TryActivate(type, constructorInfo, out var result))
                return result;
            throw Errors.CannotActivate(type);
        }

        public static bool TryActivate(this IServiceProvider services, 
            Type type, ConstructorInfo constructorInfo, [NotNullWhen(true)] out object? result)
        {
            var args = constructorInfo.GetParameters();
            var argValues = new object[args.Length];
            for (var i = 0; i < args.Length; i++) {
                var arg = args[i];
                var value = services.GetService(arg.ParameterType);
                if (value == null) {
                    result = null;
                    return false;
                }
                argValues[i] = value;
            }
            result = constructorInfo.Invoke(argValues);
            return true;
        }
    }
}
