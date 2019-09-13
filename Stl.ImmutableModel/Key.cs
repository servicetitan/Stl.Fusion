using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    [Serializable]
    [TypeConverter(typeof(KeyTypeConverter))]
    [JsonConverter(typeof(KeyJsonConverter))]
    public readonly struct Key : IEquatable<Key>
    {
        public Symbol Symbol { get; }
        public string Value => Symbol.Value;

        public Key(Symbol symbol) => Symbol = symbol;
        public Key(string value) => Symbol = value;

        public override string ToString() => $"{GetType().Name}({Value})";
        
        // Conversion
        
        public static implicit operator Key(Symbol source) => new Key(source);
        public static implicit operator Key(string source) => new Key(source);
        public static implicit operator string(Key source) => source.Value;

        // Operators
        
        public static SymbolPath operator +(SymbolPath path, Key key) => path + key.Symbol;

        // Equality

        public bool Equals(Key other) => Symbol == other.Symbol;
        public override bool Equals(object? obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => Symbol.GetHashCode();
        public static bool operator ==(Key left, Key right) => left.Equals(right);
        public static bool operator !=(Key left, Key right) => !left.Equals(right);
    }
}
