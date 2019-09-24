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
        public SymbolList Parts { get; }
        public string Value => Parts.Value;

        public Key(SymbolList list) => Parts = list;

        public override string ToString() => $"{GetType().Name}({Parts})";

        // Conversion

        public static Key Parse(string value) => new Key(SymbolList.Parse(value));

        public void Deconstruct(out SymbolList? head, out Symbol tail)
        {
            head = Parts.Head;
            tail = Parts.Tail;
        }

        public static implicit operator Key(SymbolList parts)
            => new Key(parts);
        public static implicit operator Key((SymbolList Head, Symbol Tail) source) 
            => new Key(source.Head + source.Tail);
        public static implicit operator Key((Key Head, Symbol Tail) source) 
            => new Key(source.Head.Parts + source.Tail);

        // Operators
        
        public static Key operator +(Key head, Symbol tail) => new Key(head.Parts + tail);

        // Equality

        public bool Equals(Key other) => Parts.Equals(other.Parts);
        public override bool Equals(object? obj) => obj is Key other && Equals(other);
        public override int GetHashCode() => Parts.GetHashCode();
        public static bool operator ==(Key left, Key right) => left.Equals(right);
        public static bool operator !=(Key left, Key right) => !left.Equals(right);

        // Serialization

        private Key(SerializationInfo info, StreamingContext context)
        {
            Parts = SymbolList.Parse(info.GetString(nameof(Parts))!);
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue(nameof(Parts), Parts.Value);
        }
    }
}
