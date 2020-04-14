using System;
using System.Collections.Generic;

namespace Stl.Purifier
{
    public readonly struct ComputedRef<TKey> : IEquatable<ComputedRef<TKey>>
        where TKey : notnull
    {
        public IFunction<TKey> Function { get; }
        public TKey Key { get; }
        public long Tag { get; }

        public ComputedRef(IFunction<TKey> function, TKey key, long tag)
        {
            Function = function;
            Key = key;
            Tag = tag;
        }

        public void Deconstruct(out IFunction<TKey> function, out TKey key, out long tag)
        {
            function = Function;
            key = Key;
            tag = Tag;
        }

        public override string ToString() => $"{GetType().Name}({Function}({Key}), Tag: #{Tag})";

        // Operations

        public IComputedWithTypedInput<TKey>? TryResolve()
        {
            var computed = Function.TryGetCached(Key);
            return computed.IsNull() || computed.Tag != Tag 
                ? null 
                : computed;
        }

        // Equality

        public bool Equals(ComputedRef<TKey> other) 
            => Function.Equals(other.Function) && EqualityComparer<TKey>.Default.Equals(Key, other.Key) && Tag == other.Tag;
        public override bool Equals(object? obj) 
            => obj is ComputedRef<TKey> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Function, Key, Tag);
        public static bool operator ==(ComputedRef<TKey> left, ComputedRef<TKey> right) => left.Equals(right);
        public static bool operator !=(ComputedRef<TKey> left, ComputedRef<TKey> right) => !left.Equals(right);
    }
}
