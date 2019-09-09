using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl
{
    [Serializable]
    [JsonConverter(typeof(SymbolPathJsonConverter))]
    [TypeConverter(typeof(SymbolPathTypeConverter))]
    public sealed class SymbolPath : IEquatable<SymbolPath>, IComparable<SymbolPath>, ISerializable
    {
        public static SymbolPath? Empty { get; } = (SymbolPath?) null;

        internal int HashCode { get; }
        public int SegmentCount { get; }
        public SymbolPath? Head { get; }
        public Symbol Tail { get; }
        public string Value => SymbolPathFormatter.Default.ToString(this);

        public SymbolPath(SymbolPath? head, Symbol tail)
        {
            Head = head;
            Tail = tail;
            SegmentCount = (Head?.SegmentCount ?? 0) + 1;
            HashCode = ComputeHashCode();
        }

        public SymbolPath(Symbol[] segments)
        {
            if (segments.Length == 0) 
                throw new ArgumentOutOfRangeException(nameof(segments));
            SymbolPath? head = null;
            for (var index = 0; index < segments.Length - 1; index++) 
                head = new SymbolPath(head, segments[index]);
            Head = head;
            Tail = segments[^1];
            SegmentCount = (Head?.SegmentCount ?? 0) + 1;
            HashCode = ComputeHashCode();
        }

        public SymbolPath(string value)
        {
            var parsed = Parse(value);
            Head = parsed.Head;
            Tail = parsed.Tail;
            SegmentCount = parsed.SegmentCount;
            HashCode = parsed.HashCode;
        }
        
        public SymbolPath Concat(Symbol tail) 
            => new SymbolPath(this, tail);
        public SymbolPath Concat(SymbolPath other) 
            => other.GetSegments().Aggregate(this, (current, tail) => current.Concat(tail));

        public static SymbolPath Parse(string value) => SymbolPathFormatter.Default.Parse(value);

        public override string ToString() => $"{GetType().Name}({Value})";

        // Conversion & operators

        public static implicit operator SymbolPath(string source) => Parse(source);
        public static implicit operator SymbolPath((SymbolPath Head, Symbol Tail) source) => new SymbolPath(source.Head, source.Tail);
        public static explicit operator string(SymbolPath source) => source.Value;
        public static SymbolPath operator +(SymbolPath first, Symbol second) => first.Concat(second);
        public static SymbolPath operator +(SymbolPath first, SymbolPath second) => first.Concat(second);

        // Equality & comparison
        
        public bool Equals(SymbolPath? other) 
            => other != null && HashCode == other.HashCode 
                && Tail == other.Tail && (Head?.Equals(other.Head) ?? other.Head == null);
        public override bool Equals(object? obj) 
            => ReferenceEquals(this, obj) || obj is SymbolPath other && Equals(other);
        public override int GetHashCode() => HashCode;
        
        public int CompareTo(SymbolPath? other)
        {
            if (other == null)
                return 1;
            if (Head == null)
                return other.Head == null ? Tail.CompareTo(other.Tail) : -1;
            var result = Head.CompareTo(other.Head);
            return result != 0 ? result : Tail.CompareTo(other.Tail);
        }
        
        // Enumeration
        
        public Symbol[] GetSegments(bool reversed = false) 
        {
            var result = new Symbol[SegmentCount];
            SymbolPath? current = this;
            if (reversed) {
                var index = 0;
                while (current != null) {
                    result[index++] = current.Tail;
                    current = current.Head;
                }
            }
            else {
                var index = SegmentCount - 1;
                while (current != null) {
                    result[index--] = current.Tail;
                    current = current.Head;
                }
            }

            return result;
        }

        // Serialization

        private SymbolPath(SerializationInfo info, StreamingContext context)
        {
            var parsed = Parse(info.GetString(nameof(Value)) ?? "");
            Head = parsed.Head;
            Tail = parsed.Tail;
            HashCode = parsed.HashCode;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => info.AddValue(nameof(Value), Value);
        
        // Private methods
        
        private int ComputeHashCode() => unchecked ((Head?.HashCode ?? 0) * 397 ^ Tail.HashCode);
    }
}
