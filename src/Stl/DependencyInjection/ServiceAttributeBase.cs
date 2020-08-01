using System;
using Microsoft.Extensions.DependencyInjection;

namespace Stl.DependencyInjection
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = true, Inherited = false)]
    public abstract class ServiceAttributeBase : Attribute
    {
        public string Scope { get; set; } = "";

        public abstract void TryRegister(IServiceCollection services, Type implementationType);
    }
}
