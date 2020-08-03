using System;
using System.Linq;
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

        public static ServiceAttributeBase[] GetAll(Type implementationType, Func<ServiceAttributeBase, bool>? filter = null)
            => ServiceInfo.For(implementationType, filter).Attributes;

        public static ServiceAttributeBase? Get(Type implementationType)
        {
            var attributes = GetAll(implementationType);
            return attributes.SingleOrDefault(a => a.Scope == ServiceScope.ManualRegistration)
                ?? attributes.SingleOrDefault(a => a.Scope == ServiceScope.Default);
        }

        public static ServiceAttributeBase? Get(Type implementationType, params Symbol[] preferredScopes)
        {
            var attributes = GetAll(implementationType);
            foreach (var scope in preferredScopes) {
                var attr = attributes.SingleOrDefault(a => a.Scope == scope);
                if (attr != null)
                    return attr;
            }
            return null;
        }
    }
}
