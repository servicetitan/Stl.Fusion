using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Services
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
        protected PluginSetInfo Plugins { get; }
        protected IPluginCache PluginCache { get; }
        protected IEnumerable<IPluginFilter> PluginFilters { get; }

        public IEnumerable<object> UntypedInstances => Instances.Cast<object>();
        public IEnumerable<TPlugin> Instances => _lazyInstances.Value;

        public PluginHandle(PluginSetInfo plugins, 
            IPluginCache pluginCache, IEnumerable<IPluginFilter> pluginFilters)
        {
            Plugins = plugins;
            PluginCache = pluginCache;
            PluginFilters = pluginFilters;
            _lazyInstances = new Lazy<TPlugin[]>(
                () => GetInstances(_ => true).ToArray(), 
                LazyThreadSafetyMode.ExecutionAndPublication);
        }

        public IEnumerable<object> GetUntypedInstances(Func<PluginInfo, bool> predicate)
        {
            return 
                from pluginImplType in 
                    Plugins.TypesByBaseTypeOrderedByDependency[typeof(TPlugin)]
                let pluginInfo = Plugins.InfoByType[pluginImplType]
                where predicate(pluginInfo) && PluginFilters.All(f => f.IsEnabled(pluginInfo))
                select PluginCache.GetOrCreate(pluginInfo.Type.Resolve()).UntypedInstance;
        }

        public IEnumerable<TPlugin> GetInstances(Func<PluginInfo, bool> predicate)
            => GetUntypedInstances(predicate).Cast<TPlugin>();
    }
}
