using System;
using System.Collections.Concurrent;
using System.Reactive;
using Castle.DynamicProxy.Generators;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Text;

namespace Stl.DependencyInjection
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class ServiceAttributeBase : Attribute
    {
        private static readonly ConcurrentDictionary<Type, Unit> IsInitialized =
            new ConcurrentDictionary<Type, Unit>();

        public string Scope { get; set; } = "";

        protected ServiceAttributeBase()
        {
            IsInitialized.GetOrAdd(GetType(), t => {
                AttributesToAvoidReplicating.Add(t);
                return default;
            });
        }

        public abstract void Register(IServiceCollection services, Type implementationType);

        public static ServiceAttributeBase[] GetAll(Type implementationType)
            => ServiceInfo.For(implementationType).Attributes;

        public static ServiceAttributeBase[] GetAll(Type implementationType, Symbol scope)
            => ServiceInfo.For(implementationType, scope).Attributes;
    }
}
