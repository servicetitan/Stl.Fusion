namespace Stl.Mathematics;

public static partial class RangeExt
{
    public static int Size(this Range<int> range)
        => range.End - range.Start;

    public static Range<int> Move(this Range<int> range, int offset)
        => new(range.Start + offset, range.End + offset);
    public static Range<int> Expand(this Range<int> range, int offset)
        => new(range.Start - offset, range.End + offset);
    public static Range<int> Resize(this Range<int> range, int size)
        => new(range.Start, range.Start + size);

    public static bool Contains(this Range<int> range, int value)
        => range.Start <= value && value < range.End;
    public static bool Contains(this Range<int> range, Range<int> containedRange)
        => range.Start <= containedRange.Start && containedRange.End <= range.End;

    public static bool Overlaps(this Range<int> range, Range<int> otherRange)
        => !range.IntersectWith(otherRange).IsEmptyOrNegative;

    public static Range<int> MinMaxWith(this Range<int> range, Range<int> other)
        => (Math.Min(range.Start, other.Start), Math.Max(range.End, other.End));
    public static Range<int> MinMaxWith(this Range<int> range, int point)
        => (Math.Min(range.Start, point), Math.Max(range.End, point));

    public static Range<int> IntersectWith(this Range<int> range, Range<int> other)
    {
        var result = new Range<int>(Math.Max(range.Start, other.Start), Math.Min(range.End, other.End));
        return result.IsEmptyOrNegative ? default : result;
    }
}
