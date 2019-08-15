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
        IEnumerable<object> UntypedImplementations { get; }
    }

    public interface IPluginHandle<out TPlugin> : IPluginHandle
    {
        IEnumerable<TPlugin> Implementations { get; }
    }

    public class PluginHandle<TPlugin> : IPluginHandle<TPlugin>
    {
        private readonly Lazy<TPlugin[]> _lazyPlugins; 
        public IEnumerable<object> UntypedImplementations => Implementations.Cast<object>();
        public IEnumerable<TPlugin> Implementations => _lazyPlugins.Value;

        public PluginHandle(IServiceProvider services)
        {
            var pluginSetInfo = services.GetService<PluginSetInfo>();
            var pluginType = typeof(TPlugin);
            if (!pluginSetInfo.Exports.Contains(pluginType))
                throw Errors.UnknownPluginType(pluginType.Name);
            _lazyPlugins = new Lazy<TPlugin[]>(
                () => pluginSetInfo
                    .ImplementationsByBaseType[pluginType]
                    .Select(t => (TPlugin) services.GetPluginImplementation(t.Resolve()))
                    .ToArray(), 
                LazyThreadSafetyMode.ExecutionAndPublication);
        }
    }
}
