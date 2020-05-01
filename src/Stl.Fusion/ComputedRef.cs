using System;
using System.Collections.Generic;

namespace Stl.Fusion
{
    public readonly struct ComputedRef : IEquatable<ComputedRef>
    {
        public IFunction Function { get; }
        public object Key { get; }

        public ComputedRef(IFunction function, object key)
        {
            Function = function;
            Key = key;
        }

        public void Deconstruct(out IFunction function, out object key)
        {
            function = Function;
            key = Key;
        }

        public override string ToString() => $"{GetType().Name}({Function}({Key}))";

        // Operations

        public IComputed? TryResolve() 
            => Function.TryGetCached(Key);

        // Equality

        public bool Equals(ComputedRef other) 
            => Function.Equals(other.Function) && Key.Equals(other.Key);
        public override bool Equals(object? obj) 
            => obj is ComputedRef other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(Function, Key);
        public static bool operator ==(ComputedRef left, ComputedRef right) => left.Equals(right);
        public static bool operator !=(ComputedRef left, ComputedRef right) => !left.Equals(right);
    }
}
