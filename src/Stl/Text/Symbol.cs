using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Text.Json.Serialization;
using Stl.Conversion;
using Stl.Text.Internal;

namespace Stl.Text
{
    [DataContract]
    [JsonConverter(typeof(SymbolJsonConverter))]
    [Newtonsoft.Json.JsonConverter(typeof(SymbolNewtonsoftJsonConverter))]
    [TypeConverter(typeof(SymbolTypeConverter))]
    public readonly struct Symbol : IEquatable<Symbol>, IComparable<Symbol>,
        IConvertibleTo<string>, ISerializable
    {
        public static readonly Symbol Empty = new("");

        private readonly string? _value;
        private readonly int _hashCode;

        [DataMember(Order = 0)]
        public string Value => _value ?? "";
        public int HashCode => _hashCode;
        public bool IsEmpty => Value.Length == 0;

        public Symbol(string value)
        {
            _value = value ?? "";
            _hashCode = _value.Length == 0 ? 0 : StringComparer.Ordinal.GetHashCode(_value);
        }

        public override string ToString() => Value;

        // Conversion

        string IConvertibleTo<string>.Convert() => Value;
        public static implicit operator Symbol(string source) => new(source);
        public static implicit operator string(Symbol source) => source.Value;

        // Operators

        public static Symbol operator +(Symbol left, Symbol right) => new(left.Value + right.Value);

        // Equality & comparison

        public bool Equals(Symbol other)
            => HashCode == other.HashCode
                && StringComparer.Ordinal.Equals(Value, other.Value);
        public override bool Equals(object? obj) => obj is Symbol other && Equals(other);
        public override int GetHashCode() => HashCode;
        public int CompareTo(Symbol other) => string.CompareOrdinal(Value, other.Value);
        public static bool operator ==(Symbol left, Symbol right) => left.Equals(right);
        public static bool operator !=(Symbol left, Symbol right) => !left.Equals(right);

        // Serialization

#pragma warning disable CS8618
        private Symbol(SerializationInfo info, StreamingContext context)
        {
            _value = info.GetString(nameof(Value)) ?? "";
            _hashCode = _value.Length == 0 ? 0 : _value.GetHashCode();
        }
#pragma warning restore CS8618

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
            => info.AddValue(nameof(Value), Value);
    }
}
