using System;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;

namespace Stl.ImmutableModel
{
    [Serializable]
    [TypeConverter(typeof(KeyTypeConverter))]
    [JsonConverter(typeof(KeyJsonConverter))]
    public readonly struct Key : IEquatable<Key>, ISerializable
    {
        public SymbolPath Path { get; }
        public string Value => Path.Value;
        public LocalKey LocalKey => Path.Tail;

        public Key(string value) : this(new SymbolPath(value)) { }
        public Key(SymbolPath path) => Path = path;
        public Key(SymbolPath head, Symbol tail) => Path = head + tail;

        public override string ToString() => $"{GetType().Name}({Path})";

        // Conversion

        public void Deconstruct(out SymbolPath head, out Symbol tail)
        {
            head = Path.Head;
            tail = Path.Tail;
        }

        public static implicit operator Key(SymbolPath source)
            => new Key(source);
        public static implicit operator Key((SymbolPath Head, Symbol Tail) source) 
            => new Key(source.Head, source.Tail);
        public static implicit operator Key((Key Head, LocalKey Tail) source) 
            => new Key(source.Head.Path, source.Tail.Symbol);

        // Operators
        
        public static Key operator +(Key head, LocalKey tail) => new Key(head.Path, tail.Symbol);

        // Equality

        public bool Equals(Key other) => LocalKey == other.LocalKey;
        public override bool Equals(object? obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => LocalKey.GetHashCode();
        public static bool operator ==(Key left, Key right) => left.Equals(right);
        public static bool operator !=(Key left, Key right) => !left.Equals(right);

        // Serialization

        private Key(SerializationInfo info, StreamingContext context)
        {
            Path = new SymbolPath(info.GetString(nameof(Path))!);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Path), Path.Value);
        }
    }
}
