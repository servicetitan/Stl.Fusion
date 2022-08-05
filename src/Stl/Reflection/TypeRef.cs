using System.ComponentModel;
using Stl.Reflection.Internal;

namespace Stl.Reflection;

[DataContract]
[JsonConverter(typeof(TypeRefJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(TypeRefNewtonsoftJsonConverter))]
[TypeConverter(typeof(TypeRefTypeConverter))]
public readonly struct TypeRef : IEquatable<TypeRef>, IComparable<TypeRef>, ISerializable
{
    public static readonly TypeRef None = default;

    [DataMember(Order = 0)]
    public Symbol AssemblyQualifiedName { get; }
    public string TypeName => AssemblyQualifiedName.Value[..AssemblyQualifiedName.Value.IndexOf(',')];

    public TypeRef(Type type) : this(type.AssemblyQualifiedName!) { }
    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
    public TypeRef(Symbol assemblyQualifiedName) => AssemblyQualifiedName = assemblyQualifiedName;
    public TypeRef(string assemblyQualifiedName) => AssemblyQualifiedName = assemblyQualifiedName;

    public override string ToString() => AssemblyQualifiedName.Value;

    public Type? TryResolve() => Resolve(AssemblyQualifiedName);
    public Type Resolve() => Resolve(AssemblyQualifiedName)
        ?? throw Errors.TypeNotFound(AssemblyQualifiedName);

    public TypeRef TrimAssemblyVersion()
    {
        var assemblyQualifiedName = AssemblyQualifiedName.Value;
        var assemblyVersionIndex = assemblyQualifiedName.IndexOf(", Version=", StringComparison.Ordinal);
        if (assemblyVersionIndex < 0)
            return new(assemblyQualifiedName);
        var shortAssemblyQualifiedName = assemblyQualifiedName[..assemblyVersionIndex];
        return new(shortAssemblyQualifiedName);
    }

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

    public static Type? Resolve(string assemblyQualifiedName)
        => Type.GetType(assemblyQualifiedName, false, false);

    // Serialization

    private TypeRef(SerializationInfo info, StreamingContext context)
        => AssemblyQualifiedName = info.GetString(nameof(AssemblyQualifiedName)) ?? "";

    void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        => info.AddValue(nameof(AssemblyQualifiedName), AssemblyQualifiedName);
}
