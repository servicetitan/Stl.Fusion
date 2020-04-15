using System;
using System.Collections.Generic;

namespace Stl.Purifier
{
    public readonly struct ComputedRef<TIn> : IEquatable<ComputedRef<TIn>>
        where TIn : notnull
    {
        public IFunction<TIn> Function { get; }
        public TIn Key { get; }
        public long Tag { get; }

        public ComputedRef(IFunction<TIn> function, TIn key, long tag)
        {
            Function = function;
            Key = key;
            Tag = tag;
        }

        public void Deconstruct(out IFunction<TIn> function, out TIn key, out long tag)
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

        public bool Equals(ComputedRef<TIn> other) 
            => Function.Equals(other.Function) && EqualityComparer<TIn>.Default.Equals(Key, other.Key) && Tag == other.Tag;
        public override bool Equals(object? obj) 
            => obj is ComputedRef<TIn> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Function, Key, Tag);
        public static bool operator ==(ComputedRef<TIn> left, ComputedRef<TIn> right) => left.Equals(right);
        public static bool operator !=(ComputedRef<TIn> left, ComputedRef<TIn> right) => !left.Equals(right);
    }
}
