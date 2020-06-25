using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace Stl
{
    public readonly struct LTagged<T> : IEquatable<LTagged<T>>
    {
        public readonly T Value;
        public readonly LTag LTag;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public LTagged(T value, LTag lTag)
        {
            Value = value;
            LTag = lTag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator LTagged<T>((T Value, LTag LTag) source) 
            => new LTagged<T>(source.Value, source.LTag);

        public override string ToString() => $"{Value} {LTag}";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out T value, out LTag lTag)
        {
            value = Value;
            lTag = LTag;
        }

        // Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(LTagged<T> other) 
            => LTag.Equals(other.LTag) && EqualityComparer<T>.Default.Equals(Value, other.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override bool Equals(object? obj) 
            => obj is LTagged<T> other && Equals(other);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public override int GetHashCode() 
            => HashCode.Combine(Value, LTag);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(LTagged<T> left, LTagged<T> right) 
            => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(LTagged<T> left, LTagged<T> right) 
            => !left.Equals(right);
    }
}
