namespace Stl.Mathematics;

public static partial class RangeExt
{
    public static TimeSpan Size(this Range<Moment> range)
        => range.End - range.Start;

    public static Range<Moment> Move(this Range<Moment> range, TimeSpan offset)
        => new(range.Start + offset, range.End + offset);
    public static Range<Moment> Expand(this Range<Moment> range, TimeSpan offset)
        => new(range.Start - offset, range.End + offset);
    public static Range<Moment> Resize(this Range<Moment> range, TimeSpan size)
        => new(range.Start, range.Start + size);

    public static bool Contains(this Range<Moment> range, Moment value)
        => range.Start <= value && value < range.End;
    public static bool Contains(this Range<Moment> range, Range<Moment> containedRange)
        => range.Start <= containedRange.Start && containedRange.End <= range.End;

    public static bool Overlaps(this Range<Moment> range, Range<Moment> otherRange)
        => range.IntersectWith(otherRange).Size() > TimeSpan.Zero;

    public static Range<Moment> MinMaxWith(this Range<Moment> range, Range<Moment> other)
        => (Moment.Min(range.Start, other.Start), Moment.Max(range.End, other.End));
    public static Range<Moment> MinMaxWith(this Range<Moment> range, Moment point)
        => (Moment.Min(range.Start, point), Moment.Max(range.End, point));

    public static Range<Moment> IntersectWith(this Range<Moment> range, Range<Moment> other)
    {
        var result = new Range<Moment>(Moment.Max(range.Start, other.Start), Moment.Min(range.End, other.End));
        return result.Size() < TimeSpan.Zero ? default : result;
    }
}
