using System;
using System.Collections.Generic;

namespace Stl.Purifier
{
    public readonly struct ComputationRef<TKey> : IEquatable<ComputationRef<TKey>>
    {
        public IFunction<TKey> Function { get; }
        public TKey Key { get; }
        public long Tag { get; }

        public ComputationRef(IFunction<TKey> function, TKey key, long tag)
        {
            Function = function;
            Key = key;
            Tag = tag;
        }

        public override string ToString() => $"{GetType().Name}({Function}({Key}), Tag: #{Tag})";

        // Equality

        public bool Equals(ComputationRef<TKey> other) 
            => Function.Equals(other.Function) && EqualityComparer<TKey>.Default.Equals(Key, other.Key) && Tag == other.Tag;
        public override bool Equals(object? obj) 
            => obj is ComputationRef<TKey> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Function, Key, Tag);
        public static bool operator ==(ComputationRef<TKey> left, ComputationRef<TKey> right) => left.Equals(right);
        public static bool operator !=(ComputationRef<TKey> left, ComputationRef<TKey> right) => !left.Equals(right);
    }
}
