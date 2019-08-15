using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal
{
    public interface IPluginHandle
    {
        object UntypedPlugin { get; }
    }

    public interface IPluginHandle<out TPlugin> : IPluginHandle
        where TPlugin : notnull
    {
        TPlugin Plugin { get; }
    }
    
    public class PluginHandle<TPlugin> : IPluginHandle<TPlugin>
        where TPlugin : notnull
    {
        private readonly Lazy<TPlugin> _lazyPlugin;

        public TPlugin Plugin => _lazyPlugin.Value;
        // ReSharper disable once HeapView.BoxingAllocation
        public object UntypedPlugin => Plugin;

        public PluginHandle(IServiceProvider services)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            var pluginType = typeof(TPlugin);
            if (!pluginSetInfo.Plugins.ContainsKey(pluginType))
                throw Errors.UnknownPluginImplementationType(pluginType.Name);
            _lazyPlugin = new Lazy<TPlugin>(
                () => {
                    var rightConstructor = pluginType.GetConstructor(
                        new [] {typeof(IServiceProvider)});
                    var plugin = rightConstructor != null
                        ? Activator.CreateInstance(pluginType, services)
                        : Activator.CreateInstance(pluginType);
                    return (TPlugin) plugin!; 
                }, LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
