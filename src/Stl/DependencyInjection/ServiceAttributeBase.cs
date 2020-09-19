using System;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection.Internal;

namespace Stl.DependencyInjection
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class ServiceAttributeBase : Attribute
    {
        public string Scope { get; set; } = "";

        public abstract void Register(IServiceCollection services, Type implementationType);

        public static ServiceAttributeBase[] GetAll(Type implementationType, Func<ServiceAttributeBase, bool>? filter = null)
            => ServiceInfo.For(implementationType, filter).Attributes;
    }
}
