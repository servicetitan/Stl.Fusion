using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;
using Stl.Text;

namespace Stl.ImmutableModel.Reflection
{
    [JsonConverter(typeof(ItemKeyJsonConverter))]
    [TypeConverter(typeof(ItemKeyTypeConverter))]
    public struct ItemKey : IEquatable<ItemKey>
    {
        public Key? Key { get; }
        public Symbol Symbol { get; }

        public bool IsKey => Key != null;
        public bool IsSymbol => Key == null;

        public ItemKey(Key key) : this(key, Symbol.Empty) { }
        public ItemKey(Symbol symbol) : this(null, symbol) { }
        public ItemKey(Key? key, Symbol symbol)
        {
            if (key == null) {
                Key = null;
                Symbol = symbol;
            }
            else {
                Key = key;
                Symbol = Symbol.Empty;
            }
        }

        // Format & Parse
        
        public override string ToString() => Format();

        public string Format()
        {
            var formatter = Key.ListFormat.CreateFormatter();
            formatter.Append(Symbol.Value);
            if (Key != null)
                Key.FormatTo(ref formatter);
            formatter.AppendEnd();
            return formatter.Output;
        }

        public static ItemKey Parse(in ReadOnlySpan<char> source)
        {
            var parser = Key.ListFormat.CreateParser(source);
            parser.ParseNext();
            var symbol = parser.Item;
            var key = KeyParser.Parse(ref parser);
            return new ItemKey(key, symbol);
        }
        
        // Conversion
        
        public static implicit operator ItemKey(string symbol) => new ItemKey(symbol);
        public static implicit operator ItemKey(Symbol symbol) => new ItemKey(symbol);
        public static implicit operator ItemKey(Key key) => new ItemKey(key);

        public Key AsKey() => Key ?? throw new InvalidCastException();
        public Symbol AsSymbol() => Key != null ? throw new InvalidCastException() : Symbol;

        public void Deconstruct(out Key? key, out Symbol symbol)
        {
            key = Key;
            symbol = Symbol;
        }
        
        // Equality & comparison

        public bool Equals(ItemKey other) 
            => Symbol.Equals(other.Symbol) && Key == other.Key;
        public override bool Equals(object? obj) 
            => obj is ItemKey other && Equals(other);
        public override int GetHashCode() 
            => (Key?.GetHashCode() ?? 0) ^ Symbol.GetHashCode();
        public static bool operator ==(ItemKey left, ItemKey right) 
            => left.Equals(right);
        public static bool operator !=(ItemKey left, ItemKey right) 
            => !left.Equals(right);
    }
}
