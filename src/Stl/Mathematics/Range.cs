using MemoryPack;

namespace Stl.Mathematics;

/// <summary>
/// Defines the boundaries of a continuous span of items
/// of type <typeparamref name="T"/>.
/// </summary>
/// <typeparam name="T">Type of the elements inside the range.</typeparam>
[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
[Newtonsoft.Json.JsonObject(Newtonsoft.Json.MemberSerialization.OptOut)]
[StructLayout(LayoutKind.Sequential)]
public readonly partial struct Range<T> : IEquatable<Range<T>>
    where T : notnull
{
    /// <summary>
    /// Lower range boundary, always inclusive.
    /// </summary>
    [DataMember(Order = 0), MemoryPackOrder(0)]
    public T Start { get; }

    /// <summary>
    /// Upper range boundary, typically exclusive.
    /// </summary>
    [DataMember(Order = 1), MemoryPackOrder(1)]
    public T End { get; }

    /// <summary>
    /// Indicates whether the range is empty (has no items).
    /// </summary>
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public bool IsEmpty => EqualityComparer<T>.Default.Equals(Start, End);
    [JsonIgnore, Newtonsoft.Json.JsonIgnore, MemoryPackIgnore]
    public bool IsNegative => Comparer<T>.Default.Compare(Start, End) > 0;

    /// <summary>
    /// Creates a new range.
    /// </summary>
    /// <param name="start"><see cref="Start"/> property value.</param>
    /// <param name="end"><see cref="End"/> property value.</param>
    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public Range(T start, T end)
    {
        Start = start;
        End = end;
    }

    /// <summary>
    /// Deconstructs the range into <code>(start, end)</code> pair.
    /// </summary>
    /// <param name="start"><see cref="Start"/> property value.</param>
    /// <param name="end"><see cref="End"/> property value.</param>
    public void Deconstruct(out T start, out T end)
    {
        start = Start;
        end = End;
    }

    /// <inheritdoc />
    public override string ToString()
        => SystemJsonSerializer.Default.Write(this, GetType());

#pragma warning disable CA1000, MA0018 // Do not declare static members on generic types
    /// <summary>
    /// Parses the range using <see cref="SystemJsonSerializer"/>.
    /// </summary>
    /// <param name="value">The string representation of the range to parse.</param>
    /// <returns>Parsed range.</returns>
    public static Range<T> Parse(string value)
        => SystemJsonSerializer.Default.Read<Range<T>>(value);

    /// <summary>
    /// Implicit conversion of a 2-item <see cref="ValueTuple"/> to range.
    /// </summary>
    /// <param name="source">Source tuple.</param>
    /// <returns>A range constructed from a tuple.</returns>
    public static implicit operator Range<T>((T Start, T End) source)
        => new(source.Start, source.End);

    // Misc. operations

    public Range<T> Normalize()
        => IsNegative ? -this : this;

    // Equality

    public bool Equals(Range<T> other)
    {
        var equalityComparer = EqualityComparer<T>.Default;
        return equalityComparer.Equals(Start, other.Start) && equalityComparer.Equals(End, other.End);
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
        => obj is Range<T> other && Equals(other);
    /// <inheritdoc />
    public override int GetHashCode()
        => HashCode.Combine(Start, End);

    /// <summary>
    /// Equality operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><code>true</code> if two ranges are equal; otherwise, <code>false</code>.</returns>
    public static bool operator ==(Range<T> left, Range<T> right)
        => left.Equals(right);

    /// <summary>
    /// Inequality operator.
    /// </summary>
    /// <param name="left">Left operand.</param>
    /// <param name="right">Right operand.</param>
    /// <returns><code>true</code> if two ranges aren't equal; otherwise, <code>false</code>.</returns>
    public static bool operator !=(Range<T> left, Range<T> right)
        => !left.Equals(right);

    public static Range<T> operator -(Range<T> range)
            => new(range.End, range.Start);
}
