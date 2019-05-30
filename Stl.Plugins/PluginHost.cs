using System;
using System.Collections.Generic;
using System.Composition.Hosting;
using System.Linq;
using Serilog;
using Serilog.Core;
using Stl.Internal;

namespace Stl.Plugins
{
    public interface IPluginHost : IHasLog, IDisposable
    {
        CompositionHost? CompositionHost { get; }
        // List of plugins ordered by their assembly dependencies; this property is set first
        // during the initialization; plugins may use it during their own initialization process
        // (e.g. to build the list of their dependencies dynamically)
        IEnumerable<Plugin> PartiallyOrderedPlugins { get; }
        // All plugins, topologically ordered (less dependent ones go first)
        IEnumerable<Plugin> Plugins { get; }
        // All injection points, topologically ordered (IPs from less dependent plugins go first)
        IEnumerable<InjectionPoint> InjectionPoints { get; }
    }
    
    public interface IPluginHost<out TPlugin> : IPluginHost
        where TPlugin : Plugin
    {
        new IEnumerable<TPlugin> PartiallyOrderedPlugins { get; }
        new IEnumerable<TPlugin> Plugins { get; }
    }

    public static class PluginHostExtensions
    {
        public static IEnumerable<TPoint> GetInjectionPoints<TPoint>(this IPluginHost host) => 
            host.InjectionPoints.Where(p => p is TPoint).Cast<TPoint>();

        public static void Inject<TPoint>(this IPluginHost host, 
            Action<TPoint> invoker)
        {
            foreach (var point in host.GetInjectionPoints<TPoint>())
                invoker.Invoke(point);
        }

        public static TState Inject<TPoint, TState>(this IPluginHost host, 
            TState initialState, Func<TPoint, TState, TState> invoker)
        {
            var state = initialState;
            foreach (var point in host.GetInjectionPoints<TPoint>())
                state = invoker.Invoke(point, state);
            return state;
        }
    }
    
    public abstract class PluginHostBase<TPlugin> : IPluginHost<TPlugin>
        where TPlugin : Plugin
    {
        public ILogger Log { get; protected set; }
        public CompositionHost? CompositionHost { get; private set; }
        IEnumerable<Plugin> IPluginHost.PartiallyOrderedPlugins => PartiallyOrderedPlugins;
        public IEnumerable<TPlugin> PartiallyOrderedPlugins { get; private set; } = Enumerable.Empty<TPlugin>();
        IEnumerable<Plugin> IPluginHost.Plugins => Plugins;
        public IEnumerable<TPlugin> Plugins { get; private set; } = Enumerable.Empty<TPlugin>();
        public IEnumerable<InjectionPoint> InjectionPoints { get; private set; } = Enumerable.Empty<InjectionPoint>();

        protected PluginHostBase(ILogger? log = null) => 
            Log = log ?? Logger.None;

        protected virtual void Dispose(bool disposing)
        {
            Log.Information("Disposing.");
            var plugins = Plugins;
            Plugins = Enumerable.Empty<TPlugin>();
            foreach (var plugin in plugins)
                plugin?.Dispose();
        }
        
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
        
        protected void InitializePlugins(CompositionHost compositionHost)
        {
            CompositionHost = compositionHost;
            var allPlugins = compositionHost.GetExports<TPlugin>().ToArray();
            var pluginByType = allPlugins.ToDictionary(p => p.GetType());
            var pluginsByAssemblyName = (
                from p in allPlugins
                let t = p.GetType()
                let a = t.Assembly
                group p by a.GetName()
                ).ToDictionary(g => g.Key, g => g.ToArray());
                
            IEnumerable<TPlugin> GetAssemblyBasedDependencies(TPlugin plugin) =>
                from a in plugin.GetType().Assembly.GetReferencedAssemblies()
                where pluginsByAssemblyName.ContainsKey(a)
                from dependency in pluginsByAssemblyName[a]
                select dependency;

            IEnumerable<TPlugin> GetAllDependencies(TPlugin plugin) =>
                plugin.Dependencies
                    .Where(t => pluginByType.ContainsKey(t))
                    .Select(t => pluginByType[t])
                    .Concat(GetAssemblyBasedDependencies(plugin))
                    .Distinct();

            PartiallyOrderedPlugins = allPlugins.OrderByDependency(GetAssemblyBasedDependencies).ToArray();
            Log.Information($"Plugins found: {string.Join(", ", PartiallyOrderedPlugins)}");

            foreach (var plugin in PartiallyOrderedPlugins)
                plugin.Initialize(this);

            Plugins = PartiallyOrderedPlugins.OrderByDependency(GetAllDependencies).ToArray();
            InjectionPoints = Plugins.SelectMany(p => p.InjectionPoints).ToArray();
        }
    }
}
