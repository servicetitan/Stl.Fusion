using System;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Internal
{
    public abstract class PluginHandle
    {
        public abstract object UntypedPlugin { get; }
    }
    
    public class PluginHandle<TPlugin> : PluginHandle
    {
        private readonly Lazy<TPlugin> _lazyPlugin;

        public TPlugin Plugin => _lazyPlugin.Value;
        // ReSharper disable once HeapView.BoxingAllocation
        public override object UntypedPlugin => Plugin;

        public PluginHandle(IServiceProvider services)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            if (!pluginSetInfo.Plugins.ContainsKey(typeof(TPlugin)))
                throw Errors.UnknownPluginImplementationType(typeof(TPlugin).Name);
            _lazyPlugin = new Lazy<TPlugin>(
                () => {
                    var plugin = Activator.CreateInstance(typeof(TPlugin), services);
                    return (TPlugin) plugin; 
                }, LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
