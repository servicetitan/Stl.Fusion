using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.DependencyInjection;

namespace Stl.Fusion.Client
{
    [Serializable]
    [AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
    public class RestEaseServiceAttribute : ServiceAttributeBase
    {
        public override void Register(IServiceCollection services, Type implementationType)
            => services.AddRestEaseService(implementationType);
    }
}
