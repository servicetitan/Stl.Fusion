using System;

namespace Stl.Serialization
{
    public readonly struct TypedSerializer<T, TSerialized> : IEquatable<TypedSerializer<T, TSerialized>>
    {
        public Func<T, TSerialized> Serializer { get; }
        public Func<TSerialized, T> Deserializer { get; }

        public TypedSerializer(Func<T, TSerialized> serializer, Func<TSerialized, T> deserializer)
        {
            Serializer = serializer;
            Deserializer = deserializer;
        }

        public void Deconstruct(out Func<T, TSerialized> serializer, out Func<TSerialized, T> deserializer)
        {
            serializer = Serializer;
            deserializer = Deserializer;
        }

        // Equality

        public bool Equals(TypedSerializer<T, TSerialized> other)
            => Serializer.Equals(other.Serializer) && Deserializer.Equals(other.Deserializer);
        public override bool Equals(object? obj)
            => obj is TypedSerializer<T, TSerialized> other && Equals(other);
        public override int GetHashCode()
            => HashCode.Combine(Serializer, Deserializer);
        public static bool operator ==(TypedSerializer<T, TSerialized> left, TypedSerializer<T, TSerialized> right)
            => left.Equals(right);
        public static bool operator !=(TypedSerializer<T, TSerialized> left, TypedSerializer<T, TSerialized> right)
            => !left.Equals(right);
    }
}
