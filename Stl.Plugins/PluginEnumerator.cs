using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Stl.Caching;
using Stl.IO;
using Stl.Plugins.Metadata;
using Stl.Reflection;

namespace Stl.Plugins 
{
    public interface IPluginEnumerator
    {
        PluginSetInfo GetPluginSetInfo();
    }

    public class PluginEnumerator : CachingPluginEnumeratorBase
    {
        public string PluginDir { get; set; } = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) ?? ".";
        public string AssemblyNamePattern { get; set; } = "*.dll";
        public HashSet<Type> PluginTypes { get; } = new HashSet<Type>();

        protected override ICache<string, string> CreateCache()
            => new FileSystemCache<string, string>(GetCacheDir());

        protected virtual string GetCacheDir()
        {
            var assembly = Assembly.GetEntryAssembly() ?? Assembly.GetCallingAssembly();
            var subdirectory = PathEx.GetHashedName($"{assembly.FullName}_{assembly.Location}");
            return System.IO.Path.Combine(System.IO.Path.GetTempPath(), subdirectory);
        }

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
            var pluginTypes = new HashSet<Type>();
            var context = GetAssemblyLoadContext();
            foreach (var name in GetPluginAssemblyNames()) {
                var assembly = context.LoadFromAssemblyPath(Path.Combine(PluginDir, name));
                var pluginAttributes = assembly.GetCustomAttributes<PluginAttribute>().ToArray();
                foreach (var pluginAttribute in pluginAttributes) {
                    var pluginType = pluginAttribute.Type;
                    if (pluginType.IsAbstract || pluginType.IsNotPublic)
                        continue;
                    pluginTypes.Add(pluginType);
                }
            }
            return new PluginSetInfo(PluginTypes, pluginTypes);
        }

        protected virtual AssemblyLoadContext GetAssemblyLoadContext() 
            => AssemblyLoadContext.Default;
    }
}
