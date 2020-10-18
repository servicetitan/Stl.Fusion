using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;

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

        public static T GetOrActivate<T>(this IServiceProvider services, params object[] arguments)
            => (T) services.GetOrActivate(typeof(T));

        public static object GetOrActivate(this IServiceProvider services, Type type, params object[] arguments)
            => services.GetService(type) ?? services.Activate(type);

        // Activate

        public static T Activate<T>(this IServiceProvider services, params object[] arguments)
            => (T) services.Activate(typeof(T), arguments);

        public static object Activate(this IServiceProvider services, Type instanceType, params object[] arguments)
            => ActivatorUtilities.CreateInstance(services, instanceType, arguments);

        // GetTypeViewFactory

        public static ITypeViewFactory GetTypeViewFactory(this IServiceProvider services)
            => services.GetService<ITypeViewFactory>() ?? TypeViewFactory.Default;

        public static TypeViewFactory<TView> GetTypeViewFactory<TView>(this IServiceProvider services)
            where TView : class
            => services.GetTypeViewFactory().For<TView>();
    }
}
