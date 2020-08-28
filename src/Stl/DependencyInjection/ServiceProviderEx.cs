using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Internal;

namespace Stl.DependencyInjection
{
    public static class ServiceProviderEx
    {
        public static IServiceProvider Empty { get; } =
            new DefaultServiceProviderFactory().CreateServiceProvider(new ServiceCollection());

        // With/As helpers

        public static IServiceProvider AsServiceProvider(this Func<Type, object?> serviceProvider)
            => (FuncServiceProvider) serviceProvider;

        public static IServiceProvider WithSecondary(this IServiceProvider primary, IServiceProvider secondary)
            => new ChainingServiceProvider(primary, secondary);
        public static IServiceProvider WithSecondary(this IServiceProvider primary, Func<Type, object?> secondary)
            => new ChainingServiceProvider(primary, (FuncServiceProvider) secondary);

        public static IServiceProvider WithPrimary(this IServiceProvider secondary, IServiceProvider primary)
            => new ChainingServiceProvider(primary, secondary);
        public static IServiceProvider WithPrimary(this IServiceProvider secondary, Func<Type, object?> primary)
            => new ChainingServiceProvider((FuncServiceProvider) primary, secondary);

        public static IServiceProvider WithUnknownServiceActivator(this IServiceProvider serviceProvider)
            => new UnknownServiceActivator(serviceProvider);

        // GetOrActivate

        public static T GetOrActivate<T>(this IServiceProvider services)
            => (T) services.GetOrActivate(typeof(T));

        public static object GetOrActivate(this IServiceProvider services, Type type)
            => services.GetService(type) ?? services.Activate(type);

        // Activate

        public static T Activate<T>(this IServiceProvider services)
            => (T) services.Activate(typeof(T));

        public static object Activate(this IServiceProvider services, Type type)
        {
            // The current impl. is super slow; use with caution.
            var ctors = type.GetConstructors()
                .OrderByDescending(ci => ci.GetParameters().Length)
                .ToList();

            var primaryCtor = ctors
                .SingleOrDefault(ci => ci.GetCustomAttribute<ServiceConstructorAttribute>() != null);
            if (primaryCtor != null)
                return services.Activate(primaryCtor);

            foreach (var ctor in ctors) {
                var result = services.TryActivate(ctor);
                if (result != null)
                    return result;
            }
            throw Errors.CannotActivate(type);
        }

        public static object Activate(this IServiceProvider services, ConstructorInfo constructorInfo)
            => services.TryActivate(constructorInfo) ?? throw Errors.CannotActivate(constructorInfo.ReflectedType!);

        // TryActivate

        public static object? TryActivate(this IServiceProvider services, ConstructorInfo constructorInfo)
        {
            var args = constructorInfo.GetParameters();
            var argValues = new object[args.Length];
            for (var i = 0; i < args.Length; i++) {
                var arg = args[i];
                var value = services.GetService(arg.ParameterType);
                if (value == null)
                    return null;
                argValues[i] = value;
            }
            return constructorInfo.Invoke(argValues);
        }
    }
}
