namespace Stl.Mathematics;

public static partial class RangeExt
{
    public static bool Equals(this Range<float> range, Range<float> otherRange, float epsilon)
        => Math.Abs(range.Start - otherRange.Start) < epsilon
            && Math.Abs(range.End - otherRange.End) < epsilon;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Size(this Range<float> range)
        => range.End - range.Start;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Range<float> Move(this Range<float> range, float offset)
        => new(range.Start + offset, range.End + offset);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Range<float> Expand(this Range<float> range, float offset)
        => new(range.Start - offset, range.End + offset);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Range<float> Resize(this Range<float> range, float size)
        => new(range.Start, range.Start + size);

    public static bool Contains(this Range<float> range, float value)
        => range.Start <= value && value < range.End;
    public static bool Contains(this Range<float> range, Range<float> containedRange)
        => range.Start <= containedRange.Start && containedRange.End <= range.End;

    public static bool Overlaps(this Range<float> range, Range<float> otherRange)
        => !range.IntersectWith(otherRange).IsEmptyOrNegative;

    public static Range<float> MinMaxWith(this Range<float> range, Range<float> other)
        => (Math.Min(range.Start, other.Start), Math.Max(range.End, other.End));
    public static Range<float> MinMaxWith(this Range<float> range, float point)
        => (Math.Min(range.Start, point), Math.Max(range.End, point));

    public static Range<float> IntersectWith(this Range<float> range, Range<float> other)
    {
        var result = new Range<float>(Math.Max(range.Start, other.Start), Math.Min(range.End, other.End));
        return result.Positive();
    }
}
