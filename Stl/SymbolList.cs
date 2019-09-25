using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl
{
    [Serializable]
    [JsonConverter(typeof(SymbolListJsonConverter))]
    [TypeConverter(typeof(SymbolListTypeConverter))]
    public sealed class SymbolList : IEquatable<SymbolList>, IComparable<SymbolList>, ISerializable
    {
        public static readonly SymbolList? Null = null;
        public static readonly SymbolList Root = new SymbolList(Null, Symbol.Empty);

        internal int HashCode { get; }
        public int SegmentCount { get; }
        public SymbolList? Head { get; }
        public Symbol Tail { get; }
        public string FormattedValue => SymbolListFormatter.Default.ToString(this);

        public SymbolList(SymbolList? head, Symbol tail)
        {
            Head = head;
            Tail = tail;
            SegmentCount = (Head?.SegmentCount ?? 0) + 1;
            HashCode = ComputeHashCode();
        }

        public SymbolList(params Symbol[] segments)
        {
            if (segments.Length == 0) 
                throw new ArgumentOutOfRangeException(nameof(segments));
            SymbolList? head = null;
            for (var index = 0; index < segments.Length - 1; index++) 
                head = new SymbolList(head, segments[index]);
            Head = head;
            Tail = segments[^1];
            SegmentCount = (Head?.SegmentCount ?? 0) + 1;
            HashCode = ComputeHashCode();
        }

        public SymbolList Concat(Symbol tail) 
            => new SymbolList(this, tail);
        public SymbolList Concat(SymbolList other) 
            => other.GetSegments().Aggregate(this, (current, tail) => current.Concat(tail));

        public static SymbolList Parse(string formattedValue) 
            => SymbolListFormatter.Default.Parse(formattedValue);

        public override string ToString() => $"{GetType().Name}({FormattedValue})";

        // Conversion & operators

        public static implicit operator SymbolList((SymbolList Head, Symbol Tail) source) => new SymbolList(source.Head, source.Tail);
        public static explicit operator string(SymbolList source) => source.FormattedValue;

        // Operators

        public static SymbolList operator +(SymbolList first, Symbol second) => first.Concat(second);
        public static SymbolList operator +(SymbolList first, string second) => first.Concat(new Symbol(second));
        public static SymbolList operator +(SymbolList first, SymbolList second) => first.Concat(second);

        // Equality & comparison
        
        public bool Equals(SymbolList? other) 
            => other != null && HashCode == other.HashCode 
                && Tail == other.Tail && (Head?.Equals(other.Head) ?? other.Head == null);
        public override bool Equals(object? obj) 
            => ReferenceEquals(this, obj) || obj is SymbolList other && Equals(other);
        public override int GetHashCode() => HashCode;
        
        public int CompareTo(SymbolList? other)
        {
            if (other == null)
                return 1;
            if (Head == null)
                return other.Head == null ? Tail.CompareTo(other.Tail) : -1;
            var result = Head.CompareTo(other.Head);
            return result != 0 ? result : Tail.CompareTo(other.Tail);
        }

        // Other operations

        public bool StartsWith(SymbolList? prefix)
        {
            if (prefix == null)
                return true;
            var segmentCountDiff = SegmentCount - prefix.SegmentCount;
            if (segmentCountDiff < 0)
                return false;

            SymbolList? list = this;
            for (; segmentCountDiff > 0; segmentCountDiff--)
                list = list!.Head;
            for (; list != null; list = list!.Head, prefix = prefix!.Head) {
                if (list!.Tail != prefix!.Tail)
                    return false;
            }
            return true;
        }
        
        // Enumeration
        
        public Symbol[] GetSegments(bool reversed = false) 
        {
            var result = new Symbol[SegmentCount];
            SymbolList? current = this;
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

        private SymbolList(SerializationInfo info, StreamingContext context)
        {
            var parsed = Parse(info.GetString(nameof(FormattedValue)) ?? "");
            Head = parsed.Head;
            Tail = parsed.Tail;
            HashCode = parsed.HashCode;
        }

        void ISerializable.GetObjectData(SerializationInfo info, StreamingContext context) 
            => info.AddValue(nameof(FormattedValue), FormattedValue);
        
        // Private methods
        
        private int ComputeHashCode() => unchecked ((Head?.HashCode ?? 0) * 397 ^ Tail.HashCode);
    }
}
