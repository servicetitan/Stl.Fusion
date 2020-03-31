using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Stl.Internal;

namespace Stl
{
    public interface IOption
    {
        bool HasValue { get; }
        object? UnsafeValue { get; }
        object? Value { get; }
    }

    [Serializable]
    [DebuggerDisplay("{" + nameof(DebugValue) + "}")]
    public readonly struct Option<T> : IEquatable<Option<T>>, IOption
    {
        public bool HasValue { get; }
        public T UnsafeValue { get; }
        [JsonIgnore] public T Value { get { AssertHasValue(); return UnsafeValue; } }
        private string DebugValue => ToString();

        // ReSharper disable once HeapView.BoxingAllocation
        object? IOption.UnsafeValue => UnsafeValue;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IOption.Value => Value;

        public static Option<T> None => default;
        public static Option<T> Some(T value) => new Option<T>(true, value);

        [JsonConstructor]
        private Option(bool hasValue, T unsafeValue)
        {
            HasValue = hasValue;
            UnsafeValue = unsafeValue;
        }

        public override string ToString() 
            => HasValue ? $"Some({Value})" : "None";

        public void Deconstruct(out bool hasValue, out T value)
        {
            hasValue = HasValue;
            value = Value;
        }

        public static implicit operator Option<T>((bool HasValue, T Value) source) 
            => new Option<T>(source.HasValue, source.Value);
        public static implicit operator Option<T>(T source) => new Option<T>(true, source);
        public static explicit operator T(Option<T> source) => source.Value;

        // Useful methods

        public T ValueOr(T other) => HasValue ? UnsafeValue : other;
        public T ValueOr(Func<T> other) => HasValue ? UnsafeValue : other.Invoke();
        [return: MaybeNull]
        public Option<TCast?> CastAs<TCast>()
            where TCast : class
            => HasValue ? Option<TCast?>.Some(UnsafeValue as TCast) : default;
        public Option<TCast> Cast<TCast>()
            where TCast : T
            => HasValue ? Option<TCast>.Some((TCast) UnsafeValue!) : default;

        // Equality

        public bool Equals(Option<T> other) 
            => HasValue == other.HasValue && EqualityComparer<T>.Default.Equals(UnsafeValue, other.UnsafeValue);
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
        public static Option<T> FromClass<T>(T value)
            where T : class?
            => value != null ? Some(value) : default; 
        public static Option<T> FromStruct<T>(T? value)
            where T : struct
            => value.HasValue ? Some(value.GetValueOrDefault()) : default; 
    }
}
