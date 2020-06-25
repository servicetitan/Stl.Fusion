using System;
using System.ComponentModel;
using Newtonsoft.Json;
using Stl.ImmutableModel.Internal;
using Stl.ImmutableModel.Reflection;
using Stl.Text;

namespace Stl.ImmutableModel.Indexing
{
    [JsonConverter(typeof(NodeLinkJsonConverter))]
    [TypeConverter(typeof(NodeLinkTypeConverter))]
    public struct NodeLink : IEquatable<NodeLink>
    {
        // Ideally, it must differ from Key and ItemKey list formats --
        // otherwise it's going to quite all the delimiters there.
        private static readonly ListFormat ListFormat = new ListFormat(',', '`');
        public static readonly NodeLink None = new NodeLink(null, Symbol.Empty);

        public Key? ParentKey { get; }
        public ItemKey ItemKey { get; }

        public NodeLink(Key? parentKey, ItemKey itemKey)
        {
            ParentKey = parentKey;
            ItemKey = itemKey;
        }

        // Format & Parse
        
        public override string ToString() => Format();

        public string Format()
        {
            var formatter = ListFormat.CreateFormatter();
            formatter.Append(ParentKey.Format());
            formatter.Append(ItemKey.Format());
            formatter.AppendEnd();
            return formatter.Output;
        }

        public static NodeLink Parse(in ReadOnlySpan<char> source)
        {
            var parser = Key.ListFormat.CreateParser(source);
            parser.ParseNext();
            var parentKey = Key.Parse(parser.Item);
            parser.ParseNext();
            var itemKey = ItemKey.Parse(parser.Item);
            return new NodeLink(parentKey, itemKey);
        }
        
        // Conversion
        
        public static implicit operator NodeLink((Key ParentKey, ItemKey ItemKey) pair) 
            => new NodeLink(pair.ParentKey, pair.ItemKey);

        public void Deconstruct(out Key? parentKey, out ItemKey itemKey)
        {
            parentKey = ParentKey;
            itemKey = ItemKey;
        }

        // Equality & comparison

        public bool Equals(NodeLink other) 
            => ParentKey == other.ParentKey && ItemKey.Equals(other.ItemKey);
        public override bool Equals(object? obj) 
            => obj is NodeLink other && Equals(other);
        public override int GetHashCode() 
            => unchecked((ParentKey?.GetHashCode() ?? 0) + 347 * ItemKey.GetHashCode());
        public static bool operator ==(NodeLink left, NodeLink right) 
            => left.Equals(right);
        public static bool operator !=(NodeLink left, NodeLink right) 
            => !left.Equals(right);
    }
}
