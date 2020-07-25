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
