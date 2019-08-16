using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Stl.Plugins.Internal;

namespace Stl.Plugins
{
    public interface IPluginHandle
    {
        IEnumerable<object> UntypedInstances { get; }
    }

    public interface IPluginHandle<out TPlugin> : IPluginHandle
    {
        IEnumerable<TPlugin> Instances { get; }
    }

    public class PluginHandle<TPlugin> : IPluginHandle<TPlugin>
    {
        private readonly Lazy<TPlugin[]> _lazyInstances; 
        public IEnumerable<object> UntypedInstances => Instances.Cast<object>();
        public IEnumerable<TPlugin> Instances => _lazyInstances.Value;

        public PluginHandle(
            IPluginContainerConfiguration configuration, 
            IPluginCache pluginCache)
        {
            var pluginType = typeof(TPlugin);
            if (!configuration.Interfaces.Contains(pluginType))
                throw Errors.UnknownPluginType(pluginType.Name);
            _lazyInstances = new Lazy<TPlugin[]>(
                () => configuration
                    .Implementations
                    .TypesByBaseType[pluginType]
                    .Select(t => (TPlugin) pluginCache.GetOrCreate(t.Resolve()).UntypedInstance)
                    .ToArray(), 
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
