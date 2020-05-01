using System;

namespace Stl.Fusion
{
    public readonly struct TaggedComputedRef : IEquatable<TaggedComputedRef>
    {
        public IFunction Function { get; }
        public object Key { get; }
        public int Tag { get; }

        public TaggedComputedRef(IFunction function, object key, int tag)
        {
            Function = function;
            Key = key;
            Tag = tag;
        }

        public void Deconstruct(out IFunction function, out object key, out long tag)
        {
            function = Function;
            key = Key;
            tag = Tag;
        }

        public override string ToString() => $"{GetType().Name}({Function}({Key}), Tag: #{Tag})";

        // Operations

        public IComputed? TryResolve()
        {
            var computed = Function.TryGetCached(Key);
            return computed == null || computed.Tag != Tag ? null : computed;
        }

        // Equality

        public bool Equals(TaggedComputedRef other) 
            => Function.Equals(other.Function) && Key.Equals(other.Key) && Tag == other.Tag;
        public override bool Equals(object? obj) 
            => obj is TaggedComputedRef other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Function, Key, Tag);
        public static bool operator ==(TaggedComputedRef left, TaggedComputedRef right) => left.Equals(right);
        public static bool operator !=(TaggedComputedRef left, TaggedComputedRef right) => !left.Equals(right);
    }
}
