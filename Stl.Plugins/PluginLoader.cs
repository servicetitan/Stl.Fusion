using System;
using System.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;

namespace Stl.Plugins
{
    public static class PluginLoader
    {
        public static CompositionHost Load(
            string nameMask = "*.Plugin.dll", 
            string path = ".",
            Assembly[]? extraAssemblies = null)
        {
            var bachPath = Assembly.GetExecutingAssembly().Location;
            var pluginPath = System.IO.Path.Combine(System.IO.Path.GetDirectoryName(bachPath), path);
            var pluginFiles = nameMask
                .Split(";", StringSplitOptions.RemoveEmptyEntries)
                .SelectMany(mask => Directory.GetFiles(pluginPath, mask, SearchOption.TopDirectoryOnly))
                .Distinct()
                .OrderBy(name => name)
                .ToList();
            var assemblies = pluginFiles
                .Select(AssemblyLoadContext.Default.LoadFromAssemblyPath)
                .Concat(extraAssemblies ?? Enumerable.Empty<Assembly>())
                .Distinct()
                .ToList();
            var configuration = new ContainerConfiguration()
                .WithAssemblies(assemblies);
            return configuration.CreateContainer();
        }
    }
}
