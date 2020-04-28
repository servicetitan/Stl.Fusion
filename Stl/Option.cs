using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
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
        [MaybeNull] public T UnsafeValue { get; }
        [JsonIgnore] public T Value {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get { AssertHasValue(); return UnsafeValue!; }
        }
        private string DebugValue => ToString();

        // ReSharper disable once HeapView.BoxingAllocation
        object? IOption.UnsafeValue => UnsafeValue;
        // ReSharper disable once HeapView.BoxingAllocation
        object? IOption.Value => Value;

        public static Option<T> None => default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Some(T value) => new Option<T>(true, value);

        [JsonConstructor]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private Option(bool hasValue, T unsafeValue)
        {
            HasValue = hasValue;
            UnsafeValue = unsafeValue;
        }

        public override string ToString() 
            => IsSome(out var v) ? $"Some({v})" : "None";

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Deconstruct(out bool hasValue, [MaybeNull] out T value)
        {
            hasValue = HasValue;
            value = UnsafeValue!;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>((bool HasValue, T Value) source) 
            => new Option<T>(source.HasValue, source.Value);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Option<T>(T source) => new Option<T>(true, source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static explicit operator T(Option<T> source) => source.Value;

        // Useful methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsSome([MaybeNullWhen(false)] out T value)
        {
            value = UnsafeValue!;
            return HasValue;
        }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsNone() => !HasValue;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ValueOr(T other) => HasValue ? UnsafeValue! : other;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T ValueOr(Func<T> other) => HasValue ? UnsafeValue! : other.Invoke();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [return: MaybeNull]
        public Option<TCast?> CastAs<TCast>()
            where TCast : class
            => HasValue ? Option<TCast?>.Some(UnsafeValue as TCast) : default;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Option<TCast> Cast<TCast>()
            where TCast : T
            => HasValue ? Option<TCast>.Some((TCast) UnsafeValue!) : default;

        // Equality

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Equals(Option<T> other) 
            => HasValue == other.HasValue 
                && EqualityComparer<T>.Default.Equals(UnsafeValue!, other.UnsafeValue!);
        public override bool Equals(object? obj) 
            => obj is Option<T> other && Equals(other);
        public override int GetHashCode() => HashCode.Combine(HasValue, Value); 
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Option<T> left, Option<T> right) => left.Equals(right);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Option<T> left, Option<T> right) => !left.Equals(right);

        // Private helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void AssertHasValue()
        {
            if (!HasValue)
                throw Errors.OptionIsNone();
        }
    }

    public static class Option
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> None<T>() => default;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> Some<T>(T value) => Option<T>.Some(value);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> FromClass<T>(T value)
            where T : class?
            => value != null ? Some(value) : default; 

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Option<T> FromStruct<T>(T? value)
            where T : struct
            => value.HasValue ? Some(value.GetValueOrDefault()) : default; 
    }
}
