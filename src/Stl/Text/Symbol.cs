using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.Text
{
    [Serializable]
    [JsonConverter(typeof(SymbolJsonConverter))]
    [TypeConverter(typeof(SymbolTypeConverter))]
    public readonly struct Symbol : IEquatable<Symbol>, IComparable<Symbol>, ISerializable
    {
        public static readonly Symbol Empty = new Symbol("");

        internal int HashCode { get; }
        public string Value { get; }

        public Symbol(string value)
        {
            Value = value;
            HashCode = value?.GetHashCode() ?? 0;
        }
        
        public override string ToString() => $"{GetType().Name}({Value})";
        
        // Conversion
        
        public static implicit operator Symbol(string source) => new Symbol(source);
        public static implicit operator string(Symbol source) => source.Value;

        // Operators

        public static Symbol operator +(Symbol left, Symbol right) => new Symbol(left.Value + right.Value);

        // Equality & comparison

        public bool Equals(Symbol other) => HashCode == other.HashCode && Value == other.Value;
        public override bool Equals(object? obj) => obj is Symbol other && Equals(other);
        public override int GetHashCode() => HashCode;
        public int CompareTo(Symbol other) => string.CompareOrdinal(Value, other.Value);
        public static bool operator ==(Symbol left, Symbol right) => left.Equals(right);
        public static bool operator !=(Symbol left, Symbol right) => !left.Equals(right);

        // Serialization

        private Symbol(SerializationInfo info, StreamingContext context)
        {
            Value = info.GetString(nameof(Value)) ?? "";
            HashCode = Value.GetHashCode();
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => info.AddValue(nameof(Value), Value);
    }
}
