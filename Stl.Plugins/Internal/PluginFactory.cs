using System;

namespace Stl.Plugins.Internal
{
    public interface IPluginFactory
    {
        object Create(Type pluginType);
    }

    public class PluginFactory : IPluginFactory
    {
        protected IServiceProvider Services { get; }

        public PluginFactory(IServiceProvider services) 
            => Services = services;

        public object Create(Type pluginType)
        {
            var preferredCtor = pluginType.GetConstructor(
                new [] {typeof(IServiceProvider)});
            var instance = preferredCtor != null
                ? Activator.CreateInstance(pluginType, Services)
                : Activator.CreateInstance(pluginType);
            if (instance == null)
                throw Errors.CantCreatePluginInstance(pluginType.Name);
            return instance;
        }
    }
}
