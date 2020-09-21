using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;
using Stl.Text;

namespace Stl.DependencyInjection
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class ServiceAttributeBase : Attribute
    {
        public string Scope { get; set; } = "";

        public abstract void Register(IServiceCollection services, Type implementationType);

        public static ServiceAttributeBase[] GetAll(Type implementationType)
            => ServiceInfo.For(implementationType).Attributes;

        public static ServiceAttributeBase[] GetAll(Type implementationType, Symbol scope)
            => ServiceInfo.For(implementationType, scope).Attributes;
    }
}
