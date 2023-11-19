using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using Stl.Internal;

namespace Stl.Serialization.Internal;

#if !NET5_0
[RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#endif
public class SerializationBinder : ISerializationBinder
{
#if !NET5_0
    public static ISerializationBinder Instance { get; } = new SerializationBinder();
#else
    private static SerializationBinder? _instance;

    public static ISerializationBinder Instance {
        [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
        get => _instance ??= new SerializationBinder();
    }
#endif

    private readonly ConcurrentDictionary<(string? AssemblyName, string TypeName), Type?> _cache;
    private readonly Func<(string?, string), Type?> _resolveTypeHandler;

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    public SerializationBinder()
    {
        _resolveTypeHandler = ResolveType;
        _cache = new ConcurrentDictionary<(string?, string), Type?>();
    }

    public Type BindToType(string? assemblyName, string typeName)
        => GetType(assemblyName, typeName) ?? throw new KeyNotFoundException();

    public void BindToName(Type serializedType, out string? assemblyName, out string? typeName)
    {
        assemblyName = serializedType.GetTypeInfo().Assembly.FullName;
        typeName = serializedType.FullName;
    }

    // Protected part

    protected Type? GetType(string? assemblyName, string typeName)
        => _cache.GetOrAdd((assemblyName, typeName), _resolveTypeHandler);

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    protected virtual Type? ResolveType((string? AssemblyName, string TypeName) key)
    {
        var (assemblyName, typeName) = key;

        if (assemblyName != null) {
            var assembly = Assembly.Load(assemblyName);
            if (assembly == null)
                throw new JsonSerializationException(
                    $"Could not load assembly '{assemblyName}'.");

#pragma warning disable IL2026
            var type = assembly.GetType(typeName);
#pragma warning restore IL2026
            if (type == null) {
                if (typeName.Contains('`', StringComparison.Ordinal)) {
                    try {
                        type = ResolveGenericType(typeName, assembly);
                    }
                    catch (Exception e) {
                        throw new JsonSerializationException(
                            $"Could not find type '{typeName}' in assembly '{assembly.FullName}'.", e);
                    }
                }
                if (type == null)
                    throw new JsonSerializationException(
                        $"Could not find type '{typeName}' in assembly '{assembly.FullName}'.");
            }
            return type;
        }
        return Type.GetType(typeName);
    }

    [RequiresUnreferencedCode(UnreferencedCode.Reflection)]
    protected Type? ResolveGenericType(string typeName, Assembly assembly)
    {
        var openBracketIndex = typeName.IndexOf('[', StringComparison.Ordinal);
        string genericTypeDefName = typeName.Substring(0, openBracketIndex);
        if (openBracketIndex < 0)
            return null;

        var genericTypeDef = assembly.GetType(genericTypeDefName);
        if (genericTypeDef == null)
            return null;

        var genericTypeArguments = new List<Type>();
        var scope = 0;
        var typeArgStartIndex = 0;
        var endIndex = typeName.Length - 1;
        for (var i = openBracketIndex + 1; i < endIndex; ++i) {
            var current = typeName[i];
            switch (current) {
            case '[':
                if (scope == 0)
                    typeArgStartIndex = i + 1;
                ++scope;
                break;
            case ']':
                --scope;
                if (scope == 0) {
                    string typeArgAssemblyQualifiedName = typeName.Substring(
                        typeArgStartIndex, i - typeArgStartIndex);
                    TypeNameHelpers.SplitAssemblyQualifiedName(typeArgAssemblyQualifiedName,
                        out var typeArgAssemblyName, out var typeArgTypeName);
                    var type = GetType(typeArgAssemblyName, typeArgTypeName) ?? throw new KeyNotFoundException();
                    genericTypeArguments.Add(type);
                }
                break;
            }
        }
        return genericTypeDef.MakeGenericType(genericTypeArguments.ToArray());
    }
}
