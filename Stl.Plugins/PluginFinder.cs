using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Stl.Caching;
using Stl.IO;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins 
{
    public interface IPluginFinder
    {
        PluginSetInfo FindPlugins();
    }

    public class PluginFinder : CachingPluginFinderBase
    {
        public string PluginDir { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        public string AssemblyNamePattern { get; set; } = "*.dll";
        public bool UseCache { get; set; } = true;
        public string CacheDir { get; set; }

        public PluginFinder(ILogger? logger = null)
            : base(logger)
        {
            CacheDir = PathEx.GetApplicationTempDirectory();
        }

        protected override ICache<string, string> CreateCache()
        {
            if (!UseCache) {
                Logger.LogDebug($"Cache isn't used.");
                return new FakeCache<string, string>();
            }
            var cache = new FileSystemCache<string, string>(GetCacheDir());
            Logger.LogDebug($"Cache directory: {cache.CacheDirectory}");
            return cache;
        }

        protected virtual string GetCacheDir() => CacheDir;

        protected override string GetCacheKey()
        {
            var files = ( 
                from name in GetPluginAssemblyNames()
                let modifyDate = File.GetLastWriteTime(Path.Combine(PluginDir, name))
                select (name, modifyDate.ToFileTime())
                ).ToArray();
            return files.ToDelimitedString();
        }

        protected virtual string[] GetPluginAssemblyNames() 
            => Directory
                .EnumerateFiles(
                    PluginDir, AssemblyNamePattern, SearchOption.TopDirectoryOnly)
                .OrderBy(name => name)
                .ToArray();

#pragma warning disable 1998
        protected override async Task<PluginSetInfo> CreatePluginSetInfoAsync()
#pragma warning restore 1998
        {
            var plugins = new HashSet<Type>();
            var context = GetAssemblyLoadContext();
            foreach (var name in GetPluginAssemblyNames()) {
                var assembly = context.LoadFromAssemblyPath(Path.Combine(PluginDir, name));
                var pluginAttributes = assembly.GetCustomAttributes<PluginAttribute>().ToArray();
                foreach (var pluginAttribute in pluginAttributes) {
                    var pluginType = pluginAttribute.Type;
                    if (pluginType.IsAbstract || pluginType.IsNotPublic)
                        continue;
                    plugins.Add(pluginType);
                }
            }
            return new PluginSetInfo(plugins);
        }

        protected virtual AssemblyLoadContext GetAssemblyLoadContext() 
            => AssemblyLoadContext.Default;
    }
}
