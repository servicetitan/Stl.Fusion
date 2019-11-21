using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Internal;
using Stl.Text;

namespace Stl.Reflection 
{
    [Serializable]
    [JsonConverter(typeof(TypeRefJsonConverter))]
    [TypeConverter(typeof(TypeRefTypeConverter))]
    public struct TypeRef : IEquatable<TypeRef>, IComparable<TypeRef>, ISerializable
    {
        public Symbol AssemblyQualifiedName { get; }
        public string Name => AssemblyQualifiedName.Value.Substring(0, AssemblyQualifiedName.Value.IndexOf(','));

        public TypeRef(Type type) : this(type.AssemblyQualifiedName!) { }
        public TypeRef(string assemblyQualifiedName) => AssemblyQualifiedName = assemblyQualifiedName;

        public override string ToString() => $"{Name}";
        
        public Type? TryResolve() => Type.GetType(AssemblyQualifiedName, false, false);
        public Type Resolve() => Type.GetType(AssemblyQualifiedName, true, false)!;

        // Conversion
        
        public static implicit operator TypeRef(string type) => new TypeRef(type);
        public static implicit operator TypeRef(Type type) => new TypeRef(type.AssemblyQualifiedName!);
        public static explicit operator string(TypeRef type) => type.AssemblyQualifiedName;
        public static explicit operator Type(TypeRef type) => type.Resolve();

        // Equality & comparison

        public bool Equals(TypeRef other) => AssemblyQualifiedName == other.AssemblyQualifiedName;
        public override bool Equals(object? obj) => obj is TypeRef other && Equals(other);
        public override int GetHashCode() => AssemblyQualifiedName.HashCode;
        public int CompareTo(TypeRef other) => AssemblyQualifiedName.CompareTo(other.AssemblyQualifiedName);

        public static bool operator ==(TypeRef left, TypeRef right) => left.Equals(right);
        public static bool operator !=(TypeRef left, TypeRef right) => !left.Equals(right);
        
        // Serialization

        private TypeRef(SerializationInfo info, StreamingContext context)
        {
            AssemblyQualifiedName = info.GetString(nameof(AssemblyQualifiedName)) ?? "";
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => info.AddValue(nameof(AssemblyQualifiedName), AssemblyQualifiedName);
    }
}
