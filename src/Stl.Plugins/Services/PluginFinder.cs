using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Caching;
using Stl.Collections;
using Stl.IO;
using Stl.OS;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins.Services 
{
    public interface IPluginFinder
    {
        PluginSetInfo FindPlugins();
    }

    public class PluginFinder : CachingPluginFinderBase
    {
        private readonly ILogger _log;

        public PathString PluginDir { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        public string AssemblyNamePattern { get; set; } = "*.dll";
        public bool UseCache { get; set; } = true;
        public PathString CacheDir { get; set; }

        public PluginFinder(ILogger<PluginFinder>? log = null)
            : base(log)
        {
            _log = log ??= NullLogger<PluginFinder>.Instance;
            CacheDir = PathEx.GetApplicationTempDirectory();
        }

        protected override IAsyncCache<string, string> CreateCache()
        {
            if (!UseCache) {
                _log.LogDebug($"Cache isn't used.");
                return new ZeroCapacityCache<string, string>();
            }
            var cache = new FileSystemCache<string, string>(GetCacheDir());
            _log.LogDebug($"Cache directory: {cache.CacheDirectory}");
            return cache;
        }

        protected virtual string GetCacheDir() => CacheDir;

        protected override string GetCacheKey()
        {
            var files = ( 
                from name in GetPluginAssemblyNames()
                let modifyDate = File.GetLastWriteTime(name)
                select (name, modifyDate.ToFileTime())
                ).ToArray();
            return files.ToDelimitedString();
        }

        protected virtual PathString[] GetPluginAssemblyNames() 
            => Directory
                .EnumerateFiles(
                    PluginDir, AssemblyNamePattern, SearchOption.TopDirectoryOnly)
                .Select(PathString.New)
                .OrderBy(name => name)
                .ToArray();

#pragma warning disable 1998
        protected override async Task<PluginSetInfo> CreatePluginSetInfoAsync()
#pragma warning restore 1998
        {
            var plugins = new HashSet<Type>();
            var context = GetAssemblyLoadContext();
            foreach (var name in GetPluginAssemblyNames()) {
                var assembly = context.LoadFromAssemblyPath(name);
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
