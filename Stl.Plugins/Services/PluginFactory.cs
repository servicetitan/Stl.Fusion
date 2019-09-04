using System;
using Stl.Extensibility;

namespace Stl.Plugins.Services
{
    public interface IPluginFactory
    {
        object? Create(Type pluginType);
    }

    public class PluginFactory : IPluginFactory
    {
        protected IServiceProvider Services { get; }

        public PluginFactory(IServiceProvider services) 
            => Services = services;

        public virtual object? Create(Type pluginType)
            => Services.Activate(pluginType);
    }
}
