using System;
using System.Collections.Concurrent;
using System.Reactive;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Text;

namespace Stl.DependencyInjection
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class ServiceAttributeBase : Attribute
    {
        private static readonly ConcurrentDictionary<Type, Unit> IsInitialized = new();

        public string Scope { get; set; } = "";

        public abstract void Register(IServiceCollection services, Type implementationType);

        public static ServiceAttributeBase[] GetAll(Type implementationType)
            => ServiceInfo.For(implementationType).Attributes;

        public static ServiceAttributeBase[] GetAll(Type implementationType, Symbol scope)
            => ServiceInfo.For(implementationType, scope).Attributes;
    }
}
