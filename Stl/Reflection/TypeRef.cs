using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.Reflection 
{
    [Serializable]
    [JsonConverter(typeof(TypeRefJsonConverter))]
    [TypeConverter(typeof(TypeRefTypeConverter))]
    public struct TypeRef : IEquatable<TypeRef>, IComparable<TypeRef>
    {
        private const int UnknownHashCode = 0;
        private const int UnknownHashCodeSubstitute = -1;

        [NonSerialized]
        private int _hashCode;

        public string AssemblyQualifiedName { get; }
        public string Name => AssemblyQualifiedName.Substring(0, AssemblyQualifiedName.IndexOf(','));

        public TypeRef(Type type) : this(type.AssemblyQualifiedName!) { }
        public TypeRef(string assemblyQualifiedName)
        {
            AssemblyQualifiedName = assemblyQualifiedName;
            _hashCode = UnknownHashCode;
        }

        public override string ToString() => $"{Name}";
        
        public Type? TryResolve() => Type.GetType(AssemblyQualifiedName, false, false);
        public Type Resolve() => Type.GetType(AssemblyQualifiedName, true, false)!;

        // Conversion 
        public static implicit operator TypeRef(string type) => new TypeRef(type);
        public static implicit operator TypeRef(Type type) => new TypeRef(type.AssemblyQualifiedName!);
        public static explicit operator string(TypeRef type) => type.AssemblyQualifiedName;
        public static explicit operator Type(TypeRef type) => type.Resolve();

        #region Equality & Comparison

        public bool Equals(TypeRef other) 
            => GetHashCode() == other.GetHashCode() 
                && AssemblyQualifiedName == other.AssemblyQualifiedName;

        public override bool Equals(object? obj) 
            => obj is TypeRef other && Equals(other);

        public override int GetHashCode()
        {
            if (_hashCode != UnknownHashCode)
                return _hashCode;
            var hashCode =  AssemblyQualifiedName?.GetHashCode() ?? UnknownHashCodeSubstitute;
            if (hashCode == UnknownHashCode)
                hashCode = UnknownHashCodeSubstitute;
            _hashCode = hashCode;
            return hashCode;
        }

        public int CompareTo(TypeRef other) 
            => string.Compare(
                AssemblyQualifiedName, 
                other.AssemblyQualifiedName, 
                StringComparison.Ordinal);

        public static bool operator ==(TypeRef left, TypeRef right) => left.Equals(right);
        public static bool operator !=(TypeRef left, TypeRef right) => !left.Equals(right);

        #endregion
    }
}
