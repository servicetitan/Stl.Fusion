namespace Stl.Mathematics;

public static partial class RangeExt
{
    public static TimeSpan Size(this Range<TimeSpan> range)
        => range.End - range.Start;

    public static Range<TimeSpan> Move(this Range<TimeSpan> range, TimeSpan offset)
        => new(range.Start + offset, range.End + offset);
    public static Range<TimeSpan> Expand(this Range<TimeSpan> range, TimeSpan offset)
        => new(range.Start - offset, range.End + offset);
    public static Range<TimeSpan> Resize(this Range<TimeSpan> range, TimeSpan size)
        => new(range.Start, range.Start + size);

    public static bool Contains(this Range<TimeSpan> range, TimeSpan value)
        => range.Start <= value && value < range.End;
    public static bool Contains(this Range<TimeSpan> range, Range<TimeSpan> containedRange)
        => range.Start <= containedRange.Start && containedRange.End <= range.End;

    public static bool Overlaps(this Range<TimeSpan> range, Range<TimeSpan> otherRange)
        => range.IntersectWith(otherRange).Size() > TimeSpan.Zero;

    public static Range<TimeSpan> MinMaxWith(this Range<TimeSpan> range, Range<TimeSpan> other)
        => (TimeSpanExt.Min(range.Start, other.Start), TimeSpanExt.Max(range.End, other.End));
    public static Range<TimeSpan> MinMaxWith(this Range<TimeSpan> range, TimeSpan point)
        => (TimeSpanExt.Min(range.Start, point), TimeSpanExt.Max(range.End, point));

    public static Range<TimeSpan> IntersectWith(this Range<TimeSpan> range, Range<TimeSpan> other)
    {
        var result = new Range<TimeSpan>(TimeSpanExt.Max(range.Start, other.Start), TimeSpanExt.Min(range.End, other.End));
        return result.Size() < TimeSpan.Zero ? default : result;
    }
}
