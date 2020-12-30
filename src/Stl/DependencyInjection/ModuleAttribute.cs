using System;
using Microsoft.Extensions.DependencyInjection;
using Stl.Reflection;

namespace Stl.DependencyInjection
{
    [Serializable]
    public class ModuleAttribute : ServiceAttributeBase
    {
        public override void Register(IServiceCollection services, Type implementationType)
        {
            var module = (IModule) implementationType.CreateInstance();
            module.Services = services;
            module.ConfigureServices();
        }
    }
}
