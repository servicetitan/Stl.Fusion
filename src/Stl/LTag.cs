using System.ComponentModel;
using Stl.Internal;

namespace Stl;

/// <summary>
/// Encapsulates <see cref="Int64"/>-typed version.
/// </summary>
[DataContract, MemoryPackable]
[JsonConverter(typeof(LTagJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(LTagNewtonsoftJsonConverter))]
[TypeConverter(typeof(LTagTypeConverter))]
public readonly partial struct LTag : IEquatable<LTag>
{
    private const int StringRadix = 62;
    public static readonly LTag Default = default;

    /// <summary>
    /// Version value.
    /// </summary>
    [DataMember(Order = 0)]
    public long Value { get; }
    /// <summary>
    /// Indicates whether this version is a special one.
    /// Special versions are just versions with negative numbers, which
    /// may or may not be treated differently.
    /// </summary>
    public bool IsSpecial => Value <= 0;

    /// <summary>
    /// Creates a new <see cref="LTag"/>.
    /// </summary>
    /// <param name="value">Its <see cref="Value"/> value.</param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public LTag(long value)
        => Value = value;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator LTag(long value) => new(value);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static explicit operator long(LTag value) => value.Value;

    /// <inheritdoc />
    public override unsafe string ToString()
    {
        Span<char> buffer = stackalloc char[16];
        buffer[0] = '@';
        var n = MathExt.FormatTo(Value, StringRadix, buffer[1..]);
        var resultSpan = buffer[..(n.Length + 1)];
#if !NETSTANDARD2_0
        return new string(resultSpan);
#else
        return new string(resultSpan.ToArray());
#endif
    }

    /// <summary>
    /// Parses a formatted LTag produced via <see cref="ToString"/> call.
    /// Throws an error if parse fails.
    /// </summary>
    /// <param name="formattedLTag">LTag string to parse.</param>
    /// <returns>Parsed LTag.</returns>
    public static LTag Parse(string? formattedLTag)
        => TryParse(formattedLTag, out var result)
            ? result
            : throw new ArgumentOutOfRangeException(nameof(formattedLTag));

    /// <summary>
    /// Parses a formatted LTag produced via <see cref="ToString"/> call.
    /// </summary>
    /// <param name="formattedLTag">LTag string to parse.</param>
    /// <param name="lTag">Parsed LTag value.</param>
    /// <returns><code>true</code> if LTag was parsed successfully; otherwise, <code>false</code>.</returns>
    public static bool TryParse(string? formattedLTag, out LTag lTag)
    {
        lTag = default;
        if (formattedLTag == null || formattedLTag.Length < 2)
            return false;
        if (formattedLTag[0] != '@')
            return false;
        if (!MathExt.TryParseInt64(formattedLTag.AsSpan(1), StringRadix, out var value))
            return false;
        lTag = new LTag(value);
        return true;
    }

    // Equality

    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(LTag other) => Value == other.Value;
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override bool Equals(object? obj) => obj is LTag other && Equals(other);
    /// <inheritdoc />
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public override int GetHashCode() => Value.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(LTag left, LTag right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(LTag left, LTag right) => !left.Equals(right);
}
