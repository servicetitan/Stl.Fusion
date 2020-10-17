using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Extensibility;
using Stl.Serialization.Internal;

namespace Stl.Serialization
{
    [Serializable]
    [JsonConverter(typeof(JsonStringJsonConverter))]
    [TypeConverter(typeof(JsonStringTypeConverter))]
    public class JsonString : IEquatable<JsonString>, IComparable<JsonString>,
        IConvertibleTo<string?>, ISerializable
    {
        public string? Value { get; }

        public JsonString(string? value) => Value = value;

        public override string? ToString() => Value;

        // Conversion

        string? IConvertibleTo<string?>.Convert() => Value;
        public static implicit operator JsonString(string? source) => new JsonString(source);
        public static implicit operator string?(JsonString source) => source.Value;

        // Operators

        public static JsonString operator +(JsonString left, JsonString right) => new JsonString(left.Value + right.Value);
        public static JsonString operator +(JsonString left, string? right) => new JsonString(left.Value + right);
        public static JsonString operator +(string? left, JsonString right) => new JsonString(left + right.Value);

        // Equality & comparison

        public bool Equals(JsonString other) => Value == other.Value;
        public override bool Equals(object? obj) => obj is JsonString other && Equals(other);
        public override int GetHashCode() => Value?.GetHashCode() ?? 0;
        public int CompareTo(JsonString other) => string.CompareOrdinal(Value, other.Value);
        public static bool operator ==(JsonString left, JsonString right) => left.Equals(right);
        public static bool operator !=(JsonString left, JsonString right) => !left.Equals(right);

        // Serialization

        private JsonString(SerializationInfo info, StreamingContext context)
            => Value = info.GetString(nameof(Value));

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            => info.AddValue(nameof(Value), Value);
    }
}
