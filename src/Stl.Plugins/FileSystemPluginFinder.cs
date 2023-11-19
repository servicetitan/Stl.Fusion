using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;
#if !NETFRAMEWORK
using System.Runtime.Loader;
#endif
using Stl.Caching;
using Stl.IO;
using Stl.Plugins.Internal;
using Stl.Plugins.Metadata;

namespace Stl.Plugins;

public class FileSystemPluginFinder : CachingPluginFinderBase
{
    public new record Options : CachingPluginFinderBase.Options
    {
        public FilePath PluginDir { get; init; } =
            Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";

        public string AssemblyNamePattern { get; init; } = "*.dll";
        public Regex ExcludedAssemblyNamesRegex { get; init; } = new(
            @"((System)|(Microsoft)|(Google)|(WindowsBase)|(mscorlib))\.(.*)\.dll",
            RegexOptions.Singleline | RegexOptions.IgnoreCase | RegexOptions.ExplicitCapture);
        public bool UseCache { get; init; } = true;
        public bool DetectIndirectAssemblyDependencies { get; init; } = true;
        public FilePath CacheDir { get; init; } = FilePath.GetApplicationTempDirectory();
    }

    public new Options Settings { get; }

    public FileSystemPluginFinder(
        Options settings,
        IPluginInfoProvider pluginInfoProvider,
        ILogger<FileSystemPluginFinder>? log = null)
        : base(settings, pluginInfoProvider, log ?? NullLogger<FileSystemPluginFinder>.Instance)
        // ReSharper disable once ConvertToPrimaryConstructor
        => Settings = settings;

    protected override IAsyncCache<string, string> CreateCache()
    {
        if (!Settings.UseCache) {
            Log.LogDebug("Cache isn't used");
            return new EmptyCache<string, string>();
        }

        var cache = new FileSystemCache<string, string>(GetCacheDir());
        Log.LogDebug("Cache directory: {CacheDirectory}", cache.CacheDirectory);
        return cache;
    }

    protected virtual string GetCacheDir()
        => Settings.CacheDir;

    protected override string GetCacheKey()
    {
        var files = (
            from name in GetPluginAssemblyNames()
            let modifyDate = File.GetLastWriteTime(name)
            select (name, modifyDate.ToFileTime())
        ).ToArray();
        return files.ToDelimitedString();
    }

    protected virtual FilePath[] GetPluginAssemblyNames()
        => Directory
            .EnumerateFiles(Settings.PluginDir, Settings.AssemblyNamePattern, SearchOption.TopDirectoryOnly)
            .Select(FilePath.New)
            .Where(path => !Settings.ExcludedAssemblyNamesRegex.IsMatch(path.Value))
            .OrderBy(path => path)
            .ToArray();

    [RequiresUnreferencedCode(UnreferencedCode.Plugins)]
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
            catch (Exception e) when (e is TypeLoadException or FileNotFoundException or FileLoadException) {
                Log.LogWarning(e, "Assembly load failed: {AssemblyName}", assemblyPath);
            }
        }

        return new PluginSetInfo(plugins,
            PluginInfoProvider,
            Settings.DetectIndirectAssemblyDependencies);
    }

#if !NETFRAMEWORK
    protected virtual AssemblyLoadContext GetAssemblyLoadContext()
        => AssemblyLoadContext.Default;
#endif
}
