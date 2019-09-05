using System;
using System.Collections.Generic;
using System.Diagnostics;
using Stl.Internal;

namespace Stl
{
    public interface IOption
    {
        bool HasValue { get; }
        object? UntypedUnsafeValue { get; }
        object? UntypedValue { get; }
    }

    [Serializable]
    [DebuggerDisplay("{" + nameof(DebugValue) + "}")]
    public readonly struct Option<T> : IEquatable<Option<T>>, IOption
    {
        public bool HasValue { get; }
        public T UnsafeValue { get; }
        public T Value { get { AssertHasValue(); return UnsafeValue; } }
        object? IOption.UntypedUnsafeValue => UnsafeValue;
        object? IOption.UntypedValue => Value;
        private string DebugValue => ToString();

        public static Option<T> None => default;
        public static Option<T> Some(T value) => new Option<T>(true, value);

        private Option(bool hasValue, T value)
        {
            HasValue = hasValue;
            UnsafeValue = value;
        }

        public override string ToString() 
            => HasValue ? $"Some({Value})" : "None";

        public void Deconstruct(out bool hasValue, out T value)
        {
            hasValue = HasValue;
            value = Value;
        }

        public static implicit operator Option<T>(T source) => new Option<T>(true, source);
        public static implicit operator Option<T>((bool HasValue, T Value) source) 
            => new Option<T>(source.HasValue, source.Value);
        public static explicit operator T(Option<T> source) => source.Value;

        // Useful methods

        public T ValueOr(T other) => HasValue ? UnsafeValue : other;

        // Equality

        public bool Equals(Option<T> other) 
            => HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(Value, other.Value);
        public override bool Equals(object? obj) 
            => obj is Option<T> other && Equals(other);
        public override int GetHashCode() 
            => unchecked ((HasValue.GetHashCode() * 397) ^ EqualityComparer<T>.Default.GetHashCode(Value));

        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

        // Private helpers

        private void AssertHasValue()
        {
            if (!HasValue)
                throw Errors.OptionIsNone();
        }
    }

    public static class Option
    {
        public static Option<T> None<T>() => default;
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);
    }
}
