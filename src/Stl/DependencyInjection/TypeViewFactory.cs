using System;
using Castle.DynamicProxy;
using Stl.DependencyInjection.Internal;
using Stl.Reflection;

namespace Stl.DependencyInjection
{
    public interface ITypeViewFactory
    {
        object Create(object implementation, Type implementationType, Type viewType);
    }

    public class TypeViewFactory : ITypeViewFactory
    {
        public static TypeViewFactory Default { get; } = new TypeViewFactory();

        protected ITypeViewProxyGenerator ProxyGenerator { get; }
        protected IInterceptor[] Interceptors { get; }

        public TypeViewFactory()
            : this(TypeViewProxyGenerator.Default, TypeViewInterceptor.Default)
        { }

        public TypeViewFactory(ITypeViewProxyGenerator proxyGenerator, TypeViewInterceptor interceptor)
        {
            ProxyGenerator = proxyGenerator;
            Interceptors = new IInterceptor[] { interceptor };
        }

        public TypeViewFactory(ITypeViewProxyGenerator proxyGenerator, IInterceptor[] interceptors)
        {
            ProxyGenerator = proxyGenerator;
            Interceptors = interceptors;
        }

        public object Create(object implementation, Type implementationType, Type viewType)
        {
            var proxyType = ProxyGenerator.GetProxyType(viewType);
            return proxyType.CreateInstance(Interceptors, implementation);
        }
    }
}
