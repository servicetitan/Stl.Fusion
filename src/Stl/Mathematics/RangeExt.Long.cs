namespace Stl.Mathematics;

public static partial class RangeExt
{
    public static long Size(this Range<long> range)
        => range.End - range.Start;

    public static Range<long> Move(this Range<long> range, long offset)
        => new(range.Start + offset, range.End + offset);
    public static Range<long> Expand(this Range<long> range, long offset)
        => new(range.Start - offset, range.End + offset);
    public static Range<long> Resize(this Range<long> range, long size)
        => new(range.Start, range.Start + size);

    public static bool Contains(this Range<long> range, long value)
        => range.Start <= value && value < range.End;
    public static bool Contains(this Range<long> range, Range<long> containedRange)
        => range.Start <= containedRange.Start && containedRange.End <= range.End;

    public static bool Overlaps(this Range<long> range, Range<long> otherRange)
        => !range.IntersectWith(otherRange).IsEmptyOrNegative;

    public static Range<long> MinMaxWith(this Range<long> range, Range<long> other)
        => (Math.Min(range.Start, other.Start), Math.Max(range.End, other.End));
    public static Range<long> MinMaxWith(this Range<long> range, long point)
        => (Math.Min(range.Start, point), Math.Max(range.End, point));

    public static Range<long> IntersectWith(this Range<long> range, Range<long> other)
    {
        var result = new Range<long>(Math.Max(range.Start, other.Start), Math.Min(range.End, other.End));
        return result.IsEmptyOrNegative ? default : result;
    }
}
