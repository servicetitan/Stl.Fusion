using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Stl.Serialization
{
    [Serializable]
    public readonly struct MaybeSerialized<T> : IEquatable<MaybeSerialized<T>>
    {
        public T Value { get; }
        [JsonIgnore] [field: NonSerialized]
        public object? SerializedValue { get; }

        public MaybeSerialized(T value, object? serializedValue = null)
        {
            Value = value;
            SerializedValue = serializedValue;
        }

        public override string ToString() 
            => $"({Value}, {SerializedValue?.GetType()?.ToString() ?? "null"})";

        public void Deconstruct(out T value, out object? serializedValue)
        {
            value = Value;
            serializedValue = SerializedValue;
        }

        public bool Equals(MaybeSerialized<T> other) => EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is MaybeSerialized<T> other && Equals(other);
        public override int GetHashCode() => EqualityComparer<T>.Default.GetHashCode(Value);
        public static bool operator ==(MaybeSerialized<T> left, MaybeSerialized<T> right) => left.Equals(right);
        public static bool operator !=(MaybeSerialized<T> left, MaybeSerialized<T> right) => !left.Equals(right);
    }
}
