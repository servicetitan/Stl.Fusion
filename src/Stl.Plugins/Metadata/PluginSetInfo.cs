using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stl.Collections;
using Stl.Plugins.Internal;
using Stl.Plugins.Services;
using Stl.Reflection;

namespace Stl.Plugins.Metadata 
{
    public class PluginSetInfo
    {
        public static PluginSetInfo Empty { get; } = 
            new PluginSetInfo(
                ImmutableDictionary<TypeRef, PluginInfo>.Empty,
                ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>>.Empty, 
                ImmutableDictionary<TypeRef, ImmutableArray<TypeRef>>.Empty);

        public ImmutableDictionary<TypeRef, PluginInfo> InfoByType { get; }
        public ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> TypesByBaseType { get; }
        public ImmutableDictionary<TypeRef, ImmutableArray<TypeRef>> TypesByBaseTypeOrderedByDependency { get; }

        [JsonConstructor]
        public PluginSetInfo(
            ImmutableDictionary<TypeRef, PluginInfo> infoByType, 
            ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>> typesByBaseType,
            ImmutableDictionary<TypeRef, ImmutableArray<TypeRef>> typesByBaseTypeOrderedByDependency)
        {
            InfoByType = infoByType;
            TypesByBaseType = typesByBaseType;
            TypesByBaseTypeOrderedByDependency = typesByBaseTypeOrderedByDependency;
        }

        public PluginSetInfo(IEnumerable<Type> plugins)
        {
            var ci = new PluginSetConstructionInfo() {
                Plugins = plugins.ToArray(),
            };
            if (ci.Plugins.Length == 0) {
                // Super important to have this case handled explicitly.
                // Otherwise the initializer for PluginContainerConfiguration.Empty
                // will fail due to recursion here & attempt to register null as a
                // singleton inside BuildContainer call below.
                InfoByType = ImmutableDictionary<TypeRef, PluginInfo>.Empty;
                TypesByBaseType = ImmutableDictionary<TypeRef, ImmutableHashSet<TypeRef>>.Empty;
                TypesByBaseTypeOrderedByDependency = ImmutableDictionary<TypeRef, ImmutableArray<TypeRef>>.Empty;
                return;
            }

            HashSet<Assembly> GetAllDependencies(Assembly assembly, HashSet<Assembly>? result = null)
            {
                result ??= new HashSet<Assembly>();
                foreach (var referenceName in assembly.GetReferencedAssemblies()) {
                    var reference = Assembly.Load(referenceName);
                    if (result.Add(reference))
                        GetAllDependencies(reference, result);
                }
                return result;
            }

            var hAssemblies = ci.Plugins.Select(t => t.Assembly).ToHashSet();
            ci.AllAssemblyRefs = hAssemblies
                .Select(a => (
                    Assembly: a, 
                    Refs: GetAllDependencies(a)
                        .Where(a => hAssemblies.Contains(a))
                        .ToHashSet()))
                .ToDictionary(p => p.Assembly, p => p.Refs);
            ci.Assemblies = hAssemblies
                .OrderByDependency(a => 
                    ci.AllAssemblyRefs.GetValueOrDefault(a) ?? Enumerable.Empty<Assembly>())
                .ToArray();
            ci.PluginFactory = new PluginHostBuilder()
                .ConfigureServices((builder, services) => {
                    services.AddSingleton<IPluginInfoQuery>(new PluginInfoQuery());
                    services.AddSingleton<IPluginFactory>(s => new QueryingPluginFactory(s));
                })
                .SetAutoStart(false)
                .Build()
                .GetRequiredService<IPluginFactory>();

            var dPlugins = new Dictionary<TypeRef, PluginInfo>();
            var dTypesByBaseType = new Dictionary<TypeRef, ImmutableHashSet<TypeRef>>();

            foreach (var plugin in ci.Plugins) {
                var pluginInfo = new PluginInfo(plugin, ci);
                dPlugins.Add(plugin, pluginInfo);
                foreach (var baseType in pluginInfo.CastableTo) {
                    var existingImpls = dTypesByBaseType.GetValueOrDefault(baseType) 
                        ?? ImmutableHashSet<TypeRef>.Empty;
                    dTypesByBaseType[baseType] = existingImpls.Add(pluginInfo.Type);
                }
            }

            var orderedPlugins = dPlugins.Values
                .OrderByDependency(p => p.AllDependencies.Select(t => dPlugins[t]))
                .ToArray();
            for (var i = 0; i < orderedPlugins.Length; i++) 
                orderedPlugins[i].OrderByDependencyIndex = i;

            InfoByType = dPlugins.ToImmutableDictionary();
            TypesByBaseType = dTypesByBaseType.ToImmutableDictionary();
            TypesByBaseTypeOrderedByDependency = dTypesByBaseType
                .Select(p => new KeyValuePair<TypeRef, ImmutableArray<TypeRef>>(
                    p.Key,
                    p.Value
                        .OrderBy(t => dPlugins[t].OrderByDependencyIndex)
                        .ToImmutableArray()))
                .ToImmutableDictionary();
        }

        public override string ToString() 
            => $"{GetType().Name} of [{InfoByType.Values.ToDelimitedString()}]";
    }
}
