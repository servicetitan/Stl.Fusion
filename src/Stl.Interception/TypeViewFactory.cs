using System;
using Castle.DynamicProxy;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;
using Stl.Interception.Interceptors;
using Stl.Reflection;

namespace Stl.Interception
{
    public interface ITypeViewFactory
    {
        object CreateView(object implementation, Type implementationType, Type viewType);
        TypeViewFactory<TView> For<TView>()
            where TView : class;
    }

    public class TypeViewFactory : ITypeViewFactory
    {
        public static TypeViewFactory Default { get; } = new(DependencyInjection.ServiceProviderEx.Empty);

        protected IServiceProvider Services { get; }
        protected ITypeViewProxyGenerator ProxyGenerator { get; }
        protected IInterceptor[] Interceptors { get; }

        public TypeViewFactory(IServiceProvider services)
            : this(
                services,
                services.GetService<ITypeViewProxyGenerator>() ?? services.Activate<TypeViewProxyGenerator>(),
                services.GetOrActivate<TypeViewInterceptor>())
        { }

        public TypeViewFactory(
            IServiceProvider services,
            ITypeViewProxyGenerator proxyGenerator,
            TypeViewInterceptor interceptor)
        {
            Services = services;
            ProxyGenerator = proxyGenerator;
            Interceptors = new IInterceptor[] { interceptor };
        }

        public TypeViewFactory(
            IServiceProvider services,
            ITypeViewProxyGenerator proxyGenerator,
            IInterceptor[] interceptors)
        {
            Services = services;
            ProxyGenerator = proxyGenerator;
            Interceptors = interceptors;
        }

        public object CreateView(object implementation, Type implementationType, Type viewType)
        {
            if (!(implementationType.IsClass || implementationType.IsInterface))
                throw new ArgumentOutOfRangeException(nameof(implementationType));
            if (!viewType.IsInterface)
                throw new ArgumentOutOfRangeException(nameof(viewType));
            var proxyType = ProxyGenerator.GetProxyType(implementationType, viewType);
            return proxyType.CreateInstance(Interceptors, implementation);
        }

        public TypeViewFactory<TView> For<TView>()
            where TView : class
            => new(this);
    }

    public readonly struct TypeViewFactory<TView>
        where TView : class
    {
        public ITypeViewFactory Factory { get; }

        public TypeViewFactory(ITypeViewFactory factory) => Factory = factory;

        public TView CreateView(Type implementationType, object implementation)
            => (TView) Factory.CreateView(implementation, implementationType, typeof(TView));

        public TView CreateView<TImplementation>(TImplementation implementation)
            where TImplementation : class
            => (TView) Factory.CreateView(implementation, typeof(TImplementation), typeof(TView));
    }
}
