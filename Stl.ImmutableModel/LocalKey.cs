using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    [Serializable]
    [TypeConverter(typeof(LocalKeyTypeConverter))]
    [JsonConverter(typeof(LocalKeyJsonConverter))]
    public readonly struct LocalKey : IEquatable<LocalKey>
    {
        public Symbol Symbol { get; }
        public string Value => Symbol.Value;

        public LocalKey(Symbol symbol) => Symbol = symbol;
        public LocalKey(string value) => Symbol = value;

        public override string ToString() => $"{GetType().Name}({Value})";
        
        // Conversion
        
        public static implicit operator LocalKey(Symbol source) => new LocalKey(source);
        public static implicit operator LocalKey(string source) => new LocalKey(source);
        public static implicit operator string(LocalKey source) => source.Value;

        // Operators
        
        public static SymbolPath operator +(SymbolPath path, LocalKey localKey) => path + localKey.Symbol;

        // Equality

        public bool Equals(LocalKey other) => Symbol == other.Symbol;
        public override bool Equals(object? obj) => obj is LocalKey other && Equals(other);
        public override int GetHashCode() => Symbol.GetHashCode();
        public static bool operator ==(LocalKey left, LocalKey right) => left.Equals(right);
        public static bool operator !=(LocalKey left, LocalKey right) => !left.Equals(right);
    }
}
