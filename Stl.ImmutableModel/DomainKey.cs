using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Stl.ImmutableModel.Internal;
using Stl.Reflection;

namespace Stl.ImmutableModel
{
    [Serializable]
    [TypeConverter(typeof(DomainKeyTypeConverter))]
    public readonly struct DomainKey : IEquatable<DomainKey>, ISerializable
    {
        public Type Domain { get; }
        public Key Key { get; }

        public DomainKey(Type domain, Key key)
        {
            Domain = domain;
            Key = key;
        }

        public override string ToString() => $"{GetType().Name}({Domain}, {Key})";

        // Conversion

        public void Deconstruct(out Type domain, out Key key)
        {
            domain = Domain;
            key = Key;
        }

        public static implicit operator DomainKey((Type Domain, Key Key) source) 
            => new DomainKey(source.Domain, source.Key);

        // Operators
        
        public static SymbolPath operator +(SymbolPath path, DomainKey domainKey) => path + domainKey.Key.Symbol;

        // Equality

        public bool Equals(DomainKey other) => Key == other.Key && Domain == other.Domain;
        public override bool Equals(object? obj) => obj is DomainKey other && Equals(other);
        public override int GetHashCode() 
            => unchecked (((Domain?.GetHashCode() ?? 0) * 397) ^ Key.GetHashCode());
        public static bool operator ==(DomainKey left, DomainKey right) => left.Equals(right);
        public static bool operator !=(DomainKey left, DomainKey right) => !left.Equals(right);

        // Serialization

        private DomainKey(SerializationInfo info, StreamingContext context)
        {
            Domain = new TypeRef(info.GetString(nameof(Domain))!).Resolve();
            Key = new Key(info.GetString(nameof(Key))!);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Domain), Domain.AssemblyQualifiedName);
            info.AddValue(nameof(Key), Key.Value);
        }
    }
}
