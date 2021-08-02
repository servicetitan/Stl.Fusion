using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Caching;
using Stl.Collections;
using Stl.IO;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins
{
    public class FileSystemPluginFinder : CachingPluginFinderBase
    {
        public FileSystemPluginFinder(
            Options? options,
            IPluginInfoProvider pluginInfoProvider,
            ILogger<FileSystemPluginFinder>? log = null)
            : base(options ??= new Options(), pluginInfoProvider, log ?? NullLogger<FileSystemPluginFinder>.Instance)
        {
            PluginDir = options.PluginDir;
            AssemblyNamePattern = options.AssemblyNamePattern;
            UseCache = options.UseCache;
            CacheDir = options.CacheDir;
        }

        public PathString PluginDir { get; }
        public string AssemblyNamePattern { get; }
        public bool UseCache { get; }
        public PathString CacheDir { get; }

        protected override IAsyncCache<string, string> CreateCache()
        {
            if (!UseCache) {
                Log.LogDebug("Cache isn't used");
                return new EmptyCache<string, string>();
            }

            var cache = new FileSystemCache<string, string>(GetCacheDir());
            Log.LogDebug("Cache directory: {CacheDirectory}", cache.CacheDirectory);
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
        protected override async Task<PluginSetInfo> FindPlugins(CancellationToken cancellationToken)
#pragma warning restore 1998
        {
            var plugins = new HashSet<Type>();
#if !NETFRAMEWORK
            var context = GetAssemblyLoadContext();
#endif
            foreach (var assemblyPath in GetPluginAssemblyNames()) {
                cancellationToken.ThrowIfCancellationRequested();
                try {
#if NETFRAMEWORK
                    var assembly = Assembly.LoadFile(assemblyPath);
#else
                    var assembly = context.LoadFromAssemblyPath(assemblyPath);
#endif
                    foreach (var type in assembly.ExportedTypes) {
                        cancellationToken.ThrowIfCancellationRequested();
                        if (type.IsAbstract || type.IsNotPublic)
                            continue;
                        var attr = type.GetCustomAttribute<PluginAttribute>();
                        if (attr?.IsEnabled == true)
                            plugins.Add(type);
                    }
                }
                catch (FileNotFoundException e) {
                    Log.LogWarning(e, "Assembly load failed: {AssemblyName}", assemblyPath);
                }
                catch (FileLoadException e) {
                    Log.LogWarning(e, "Assembly load failed: {AssemblyName}", assemblyPath);
                }
            }

            return new PluginSetInfo(plugins, PluginInfoProvider);
        }

#if !NETFRAMEWORK
        protected virtual AssemblyLoadContext GetAssemblyLoadContext()
            => AssemblyLoadContext.Default;
#endif

        public new class Options : CachingPluginFinderBase.Options
        {
            public PathString PluginDir { get; set; } =
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";

            public string AssemblyNamePattern { get; set; } = "*.dll";
            public bool UseCache { get; set; } = true;
            public PathString CacheDir { get; set; } = PathEx.GetApplicationTempDirectory();
        }
    }
}
