using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins
{
    public interface IPluginHandle
    {
        IEnumerable<object> UntypedInstances { get; }
        IEnumerable<object> GetUntypedInstances(Func<PluginInfo, bool> predicate); 
    }

    public interface IPluginHandle<out TPlugin> : IPluginHandle
    {
        IEnumerable<TPlugin> Instances { get; }
        IEnumerable<TPlugin> GetInstances(Func<PluginInfo, bool> predicate); 
    }

    public class PluginHandle<TPlugin> : IPluginHandle<TPlugin>
    {
        private readonly Lazy<TPlugin[]> _lazyInstances;
        protected IPluginConfiguration Configuration { get; }
        protected IPluginCache PluginCache { get; }

        public IEnumerable<object> UntypedInstances => Instances.Cast<object>();
        public IEnumerable<TPlugin> Instances => _lazyInstances.Value;

        public PluginHandle(
            IPluginConfiguration configuration, 
            IPluginCache pluginCache)
        {
            Configuration = configuration;
            PluginCache = pluginCache;
            _lazyInstances = new Lazy<TPlugin[]>(
                () => GetInstances(_ => true).ToArray(), 
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public IEnumerable<object> GetUntypedInstances(Func<PluginInfo, bool> predicate)
        {
            if (!Configuration.Interfaces.Contains(typeof(TPlugin)))
                return Enumerable.Empty<object>();
            var pluginSetInfo = Configuration.Implementations;
            return (
                from pluginImplType in 
                    pluginSetInfo.TypesByBaseTypeOrderedByDependency[typeof(TPlugin)]
                let pluginInfo = pluginSetInfo.Plugins[pluginImplType]
                select pluginInfo
                ).Where(predicate)
                .Select(p => PluginCache.GetOrCreate(p.Type.Resolve()).UntypedInstance);
        }

        public IEnumerable<TPlugin> GetInstances(Func<PluginInfo, bool> predicate)
            => GetUntypedInstances(predicate).Cast<TPlugin>();
    }
}
