using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Microsoft.Extensions.DependencyInjection;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

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

        public PluginHandle(IServiceProvider services)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            var pluginType = typeof(TPlugin);
            if (!pluginSetInfo.Exports.Contains(pluginType))
                throw Errors.UnknownPluginType(pluginType.Name);
            _lazyInstances = new Lazy<TPlugin[]>(
                () => pluginSetInfo
                    .ImplementationsByBaseType[pluginType]
                    .Select(t => (TPlugin) services.GetPluginInstance(t.Resolve()))
                    .ToArray(), 
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
