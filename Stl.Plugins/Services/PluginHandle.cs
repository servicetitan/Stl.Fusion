using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Threading;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins.Services
{
    public interface IPluginHandle
    {
        IEnumerable<object> Instances { get; }
        IEnumerable<object> GetInstances(Func<PluginInfo, bool> predicate); 
    }

    public interface IPluginHandle<out TPlugin> : IPluginHandle
    {
        new IEnumerable<TPlugin> Instances { get; }
        new IEnumerable<TPlugin> GetInstances(Func<PluginInfo, bool> predicate); 
    }

    public class PluginHandle<TPlugin> : IPluginHandle<TPlugin>
    {
        private readonly Lazy<TPlugin[]> _lazyInstances;
        protected PluginSetInfo Plugins { get; }
        protected IPluginCache PluginCache { get; }
        protected IEnumerable<IPluginFilter> PluginFilters { get; }

        IEnumerable<object> IPluginHandle.Instances => Instances.Cast<object>();
        public IEnumerable<TPlugin> Instances => _lazyInstances.Value;

        public PluginHandle(PluginSetInfo plugins, 
            IPluginCache pluginCache, IEnumerable<IPluginFilter> pluginFilters)
        {
            Plugins = plugins;
            PluginCache = pluginCache;
            PluginFilters = pluginFilters;
            _lazyInstances = new Lazy<TPlugin[]>(
                () => GetInstances(_ => true).Cast<TPlugin>().ToArray());
        }

        IEnumerable<object> IPluginHandle.GetInstances(Func<PluginInfo, bool> predicate) 
            => GetInstances(predicate);
        IEnumerable<TPlugin> IPluginHandle<TPlugin>.GetInstances(Func<PluginInfo, bool> predicate)
            => GetInstances(predicate).Cast<TPlugin>();
        protected IEnumerable<object> GetInstances(Func<PluginInfo, bool> predicate)
        {
            var requestedType = typeof(TPlugin);
            var pluginImplTypes = Plugins.TypesByBaseTypeOrderedByDependency.GetValueOrDefault(requestedType);
            if (pluginImplTypes == null)
                return Enumerable.Empty<object>();

            var pluginInfos = (
                from pluginImplType in pluginImplTypes
                let pi = Plugins.InfoByType[pluginImplType]
                where predicate(pi) && PluginFilters.All(f => f.IsEnabled(pi))
                select pi);

            if (typeof(ISingletonPlugin).IsAssignableFrom(requestedType)) {
                var lPluginInfos = pluginInfos.ToList();
                if (lPluginInfos.Count == 0)
                    return lPluginInfos;
                // Let's check if we can unambiguously identify a plugin
                // instance to return here. We assume this single instance
                // is dependent on every other instance.
                var singletonPluginInfo = lPluginInfos[^1];
                foreach (var pluginInfo in lPluginInfos) {
                    if (pluginInfo == singletonPluginInfo)
                        continue;
                    if (!singletonPluginInfo.AllDependencies.Contains(pluginInfo.Type))
                        throw Errors.MultipleSingletonPluginImplementationsFound(
                            requestedType, 
                            singletonPluginInfo.Type.Resolve(), 
                            pluginInfo.Type.Resolve());
                }
                pluginInfos = Enumerable.Repeat(singletonPluginInfo, 1);
            }

            return pluginInfos
                .Select(pi => PluginCache.GetOrCreate(pi.Type.Resolve()).Instance);
        }
    }
}
