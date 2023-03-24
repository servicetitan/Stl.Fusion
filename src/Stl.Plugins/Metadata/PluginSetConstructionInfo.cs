namespace Stl.Plugins.Metadata;

public class PluginSetConstructionInfo
{
    public Type[] Plugins { get; set; } = null!;
    public Assembly[] Assemblies { get; set; } = null!;
    public Dictionary<Assembly, HashSet<Assembly>> AssemblyDependencies { get; set; } = null!;
}
