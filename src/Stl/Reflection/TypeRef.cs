using System.ComponentModel;
using System.Text.RegularExpressions;
using Stl.Reflection.Internal;

namespace Stl.Reflection;

[StructLayout(LayoutKind.Auto)]
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[JsonConverter(typeof(TypeRefJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(TypeRefNewtonsoftJsonConverter))]
[TypeConverter(typeof(TypeRefTypeConverter))]
public readonly partial struct TypeRef : IEquatable<TypeRef>, IComparable<TypeRef>, ISerializable
{
#if NET7_0_OR_GREATER
    [GeneratedRegex(@",\s+Version=[^,]*,\s+Culture=[^,]*,\s+PublicKeyToken=[A-Za-z0-9]+")]
    private static partial Regex RemoveAssemblyVersionsReFactory();
    private static readonly Regex RemoveAssemblyVersionsRe = RemoveAssemblyVersionsReFactory();
#else
    private static readonly Regex RemoveAssemblyVersionsRe =
        new(@",\s+Version=[^,]*,\s+Culture=[^,]*,\s+PublicKeyToken=[A-Za-z0-9]+", RegexOptions.Compiled);
#endif
    private static readonly ConcurrentDictionary<Symbol, TypeRef> UnversionedAssemblyNameCache = new();
    private static readonly ConcurrentDictionary<Symbol, Type?> ResolveCache = new();

    public static readonly TypeRef None = default;

    [DataMember(Order = 0), MemoryPackOrder(0)]
    public Symbol AssemblyQualifiedName { get; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, IgnoreDataMember, MemoryPackIgnore]
    public string TypeName => AssemblyQualifiedName.Value[..AssemblyQualifiedName.Value.IndexOf(',')];

    public TypeRef(Type type)
        : this(type.AssemblyQualifiedName!) { }
    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public TypeRef(Symbol assemblyQualifiedName)
        => AssemblyQualifiedName = assemblyQualifiedName;
    public TypeRef(string assemblyQualifiedName)
        => AssemblyQualifiedName = assemblyQualifiedName;

    public override string ToString()
        => AssemblyQualifiedName.Value;

    public Type? TryResolve()
        => Resolve(AssemblyQualifiedName);
    public Type Resolve()
        => Resolve(AssemblyQualifiedName) ?? throw Errors.TypeNotFound(AssemblyQualifiedName);

    public TypeRef WithoutAssemblyVersions()
        => UnversionedAssemblyNameCache.GetOrAdd(AssemblyQualifiedName,
            static aqn => new(RemoveAssemblyVersionsRe.Replace(aqn, "")));

    // Conversion

    public static implicit operator TypeRef(string typeName) => new(typeName);
    public static implicit operator TypeRef(Type type) => new(type.AssemblyQualifiedName!);
    public static explicit operator string(TypeRef type) => type.AssemblyQualifiedName;
    public static explicit operator Type(TypeRef type) => type.Resolve();

    // Equality & comparison

    public bool Equals(TypeRef other) => AssemblyQualifiedName == other.AssemblyQualifiedName;
    public override bool Equals(object? obj) => obj is TypeRef other && Equals(other);
    public override int GetHashCode() => AssemblyQualifiedName.HashCode;
    public int CompareTo(TypeRef other) => AssemblyQualifiedName.CompareTo(other.AssemblyQualifiedName);

    public static bool operator ==(TypeRef left, TypeRef right) => left.Equals(right);
    public static bool operator !=(TypeRef left, TypeRef right) => !left.Equals(right);

    // Private methods

    public static Type? Resolve(Symbol assemblyQualifiedName)
    {
        var result = ResolveCache.GetOrAdd(assemblyQualifiedName, static aqn => Type.GetType(aqn, false, false));
        if (result == null)
            ResolveCache.TryRemove(assemblyQualifiedName, out _); // Potential memory lead / attack vector
        return result;
    }

    // Serialization

    private TypeRef(SerializationInfo info, StreamingContext context)
        => AssemblyQualifiedName = info.GetString(nameof(AssemblyQualifiedName)) ?? "";

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        => info.AddValue(nameof(AssemblyQualifiedName), AssemblyQualifiedName);
}
