using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl.Strings
{
    [Serializable]
    [JsonConverter(typeof(SymbolPathJsonConverter))]
    [TypeConverter(typeof(SymbolPathTypeConverter))]
    public sealed class Path : IEquatable<Path>, IComparable<Path>, ISerializable
    {
        internal int HashCode { get; }
        public int SegmentCount { get; }
        public Path? Head { get; }
        public Symbol Tail { get; }
        public string Value => PathFormatter.Default.ToString(this);

        public Path(Path? head, Symbol tail)
        {
            Head = head;
            Tail = tail;
            SegmentCount = (Head?.SegmentCount ?? 0) + 1;
            HashCode = ComputeHashCode();
        }

        public Path(Symbol[] segments)
        {
            if (segments.Length == 0) 
                throw new ArgumentOutOfRangeException(nameof(segments));
            Path? head = null;
            for (var index = 0; index < segments.Length - 1; index++) 
                head = new Path(head, segments[index]);
            Head = head;
            Tail = segments[^1];
            SegmentCount = (Head?.SegmentCount ?? 0) + 1;
            HashCode = ComputeHashCode();
        }

        public Path(string value)
        {
            var parsed = Parse(value);
            Head = parsed.Head;
            Tail = parsed.Tail;
            SegmentCount = parsed.SegmentCount;
            HashCode = parsed.HashCode;
        }
        
        public static Path Parse(string value) => PathFormatter.Default.Parse(value);

        public override string ToString() => $"{GetType().Name}({Value})";

        // Conversion

        public static implicit operator Path(string source) => Parse(source);
        public static implicit operator Path((Path Head, Symbol Tail) source) => new Path(source.Head, source.Tail);
        public static explicit operator string(Path source) => source.Value;

        // Equality & comparison
        
        public bool Equals(Path other) 
            => HashCode == other.HashCode && Tail == other.Tail && Head == other.Head;
        public override bool Equals(object? obj) 
            => ReferenceEquals(this, obj) || obj is Path other && Equals(other);
        public override int GetHashCode() => HashCode;
        
        public int CompareTo(Path? other)
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
            Path? current = this;
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

        private Path(SerializationInfo info, StreamingContext context)
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