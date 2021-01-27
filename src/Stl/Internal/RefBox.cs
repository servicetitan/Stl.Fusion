using System;
using System.Runtime.CompilerServices;

namespace Stl.Internal
{
    // A wrapper around reference type T that relies on reference equality
    // instead of EqualityComparer<T>.Default to compare the instances.
    public readonly struct RefBox<T> : IEquatable<RefBox<T>>
        where T : class?
    {
        public T Target { get; }

        public RefBox(T target) => Target = target;

        public override string ToString()
            => $"{GetType().Name}({Target?.ToString() ?? "␀"})";

        // Equality

        public bool Equals(RefBox<T> other)
            => ReferenceEquals(Target, other.Target);
        public override bool Equals(object? obj)
            => obj is RefBox<T> other && Equals(other);
        public override int GetHashCode()
            => RuntimeHelpers.GetHashCode(Target!);
        public static bool operator ==(RefBox<T> left, RefBox<T> right)
            => left.Equals(right);
        public static bool operator !=(RefBox<T> left, RefBox<T> right)
            => !left.Equals(right);
    }

    public static class RefBox
    {
        public static RefBox<T> New<T>(T value)
            where T : class?
            => new(value);
    }
}
