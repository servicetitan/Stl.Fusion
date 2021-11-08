using Newtonsoft.Json;
using Stl.Plugins.Internal;

namespace Stl.Plugins.Metadata;

public class PluginInfo
{
    public TypeRef Type { get; protected set; }
    public ImmutableArray<TypeRef> Ancestors { get; protected set; }
    public ImmutableArray<TypeRef> Interfaces { get; protected set; }
    public ImmutableHashSet<TypeRef> CastableTo { get; protected set; }
    public ImmutableOptionSet Capabilities { get; protected set; }
    public ImmutableHashSet<TypeRef> Dependencies { get; protected set; }
    public ImmutableHashSet<TypeRef> AllDependencies { get; protected set; }
    public int OrderByDependencyIndex { get; protected internal set; }

    [JsonConstructor]
    public PluginInfo(TypeRef type,
        ImmutableArray<TypeRef> ancestors,
        ImmutableArray<TypeRef> interfaces,
        ImmutableHashSet<TypeRef> castableTo,
        ImmutableOptionSet capabilities,
        ImmutableHashSet<TypeRef> dependencies,
        ImmutableHashSet<TypeRef> allDependencies,
        int orderByDependencyIndex)
    {
        Type = type;
        Ancestors = ancestors;
        Interfaces = interfaces;
        CastableTo = castableTo;
        Capabilities = capabilities;
        Dependencies = dependencies;
        AllDependencies = allDependencies;
        OrderByDependencyIndex = orderByDependencyIndex;
    }

    public PluginInfo(Type type, PluginSetConstructionInfo constructionInfo, IPluginInfoProvider pluginInfoProvider)
    {
        if (type.IsAbstract)
            throw Errors.PluginIsAbstract(type);
        if (type.IsNotPublic)
            throw Errors.PluginIsNonPublic(type);

        Type = type;
        Ancestors = ImmutableArray.Create(
            type.GetAllBaseTypes().Select(t => (TypeRef) t).ToArray());
        Interfaces = ImmutableArray.Create(
            type.GetInterfaces().Select(t => (TypeRef) t).ToArray());
        CastableTo = ImmutableHashSet.Create(
            Ancestors.AddRange(Interfaces).Add(type).ToArray());
        Capabilities = pluginInfoProvider.GetCapabilities(type);
        Dependencies = pluginInfoProvider.GetDependencies(type);
        var allAssemblyRefs = constructionInfo.AllAssemblyRefs[type.Assembly];
        AllDependencies = constructionInfo.Plugins
            .Where(p => p != type && (
                allAssemblyRefs.Contains(p.Assembly) ||
                CastableTo.Contains(p)))
            .Select(t => (TypeRef) t)
            .Concat(Dependencies)
            .ToImmutableHashSet();
    }

    public override string ToString() => $"{GetType().Name}({Type})";
}
