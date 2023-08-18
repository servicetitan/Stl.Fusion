using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Stl.Internal;

namespace Stl;

/// <summary>
/// Describes an optional value ("option" or "maybe"-like type).
/// </summary>
public interface IOption
{
    /// <summary>
    /// Indicates whether an option has <see cref="Value"/>.
    /// </summary>
    bool HasValue { get; }
    /// <summary>
    /// Retrieves option's value. Throws <see cref="InvalidOperationException"/> in case option doesn't have one.
    /// </summary>
    object? Value { get; }
}

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[DebuggerDisplay("{" + nameof(DebugValue) + "}")]
[StructLayout(LayoutKind.Auto)]
public readonly partial struct Option<T> : IEquatable<Option<T>>, IOption
{
    /// <inheritdoc />
    [DataMember(Order = 0), MemoryPackOrder(0)]
    public bool HasValue { get; }
    /// <summary>
    /// Retrieves option's value. Returns <code>default(T)</code> in case option doesn't have one.
    /// </summary>
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T? ValueOrDefault { get; }
    /// <summary>
    /// Retrieves option's value. Throws <see cref="InvalidOperationException"/> in case option doesn't have one.
    /// </summary>
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public T Value {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        get { AssertHasValue(); return ValueOrDefault!; }
    }

    /// <inheritdoc />
    // ReSharper disable once HeapView.BoxingAllocation
    object? IOption.Value => Value;
    private string DebugValue => ToString();

    /// <summary>
    /// Returns an option of type <typeparamref name="T"/> with no value.
    /// </summary>
    public static Option<T> None => default;
    /// <summary>
    /// Creates an option of type <typeparamref name="T"/> with the specified value.
    /// </summary>
    /// <param name="value">Option's value.</param>
    /// <returns>A newly created option.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Some(T value) => new(true, value);

    /// <summary>
    /// Constructor.
    /// </summary>
    /// <param name="hasValue"><see cref="HasValue"/> value.</param>
    /// <param name="valueOrDefault"><see cref="ValueOrDefault"/> value.</param>
    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option(bool hasValue, T? valueOrDefault)
    {
        HasValue = hasValue;
        ValueOrDefault = valueOrDefault;
    }

    /// <inheritdoc />
    public override string ToString()
        => IsSome(out var v) ? $"Some({v})" : "None";

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Deconstruct(out bool hasValue, [MaybeNullWhen(false)] out T value)
    {
        hasValue = HasValue;
        value = ValueOrDefault!;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Option<T>((bool HasValue, T Value) source)
        => new(source.HasValue, source.Value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Option<T>(T source) => new(true, source);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator T(Option<T> source) => source.Value;

    // Useful methods

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsSome([MaybeNullWhen(false)] out T value)
    {
        value = ValueOrDefault!;
        return HasValue;
    }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool IsNone() => !HasValue;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TCast?> CastAs<TCast>()
        where TCast : class
        => HasValue ? Option<TCast?>.Some(ValueOrDefault as TCast) : default;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Option<TCast> Cast<TCast>()
    {
        if (!HasValue)
            return Option.None<TCast>();
        if (ValueOrDefault is TCast value)
            return Option.Some(value);
        throw new InvalidCastException();
    }

    // Equality

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Option<T> other)
        => HasValue == other.HasValue
            && EqualityComparer<T>.Default.Equals(ValueOrDefault!, other.ValueOrDefault!);
    public override bool Equals(object? obj)
        => obj is Option<T> other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(HasValue, ValueOrDefault!);
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

/// <summary>
/// Helper methods related to <see cref="Option{T}"/> type.
/// </summary>
public static class Option
{
    /// <summary>
    /// Returns an option of type <typeparamref name="T"/> with no value.
    /// </summary>
    /// <typeparam name="T">Option type.</typeparam>
    /// <returns>Option with no value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> None<T>() => default;

    /// <summary>
    /// Creates an option of type <typeparamref name="T"/> with the specified value.
    /// </summary>
    /// <param name="value">Option's value.</param>
    /// <typeparam name="T">Option type.</typeparam>
    /// <returns>Option with the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> Some<T>(T value) => Option<T>.Some(value);

    /// <summary>
    /// Creates an option from the nullable reference.
    /// Returns <see cref="None{T}"/> when <paramref name="value"/> is <code>null</code>.
    /// </summary>
    /// <param name="value">Option's value.</param>
    /// <typeparam name="T">Option type.</typeparam>
    /// <returns>An option with the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> FromClass<T>(T value)
        where T : class?
        => value != null ? Some(value) : default;

    /// <summary>
    /// Creates an option from the nullable struct.
    /// Returns <see cref="None{T}"/> when <paramref name="value"/> is <code>null</code>.
    /// </summary>
    /// <param name="value">Option's value.</param>
    /// <typeparam name="T">Option type.</typeparam>
    /// <returns>An option with the specified value.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Option<T> FromStruct<T>(T? value)
        where T : struct
        => value.HasValue ? Some(value.GetValueOrDefault()) : default;
}
