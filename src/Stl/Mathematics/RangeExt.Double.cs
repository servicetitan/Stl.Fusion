namespace Stl.Mathematics;

public static partial class RangeExt
{
    public static bool Equals(this Range<double> range, Range<double> otherRange, double epsilon)
        => Math.Abs(range.Start - otherRange.Start) < epsilon
            && Math.Abs(range.End - otherRange.End) < epsilon;

    public static double Size(this Range<double> range)
        => range.End - range.Start;

    public static Range<double> Move(this Range<double> range, double offset)
        => new(range.Start + offset, range.End + offset);
    public static Range<double> Expand(this Range<double> range, double offset)
        => new(range.Start - offset, range.End + offset);
    public static Range<double> Resize(this Range<double> range, double size)
        => new(range.Start, range.Start + size);

    public static bool Contains(this Range<double> range, double value)
        => range.Start <= value && value < range.End;
    public static bool Contains(this Range<double> range, Range<double> containedRange)
        => range.Start <= containedRange.Start && containedRange.End <= range.End;

    public static bool Overlaps(this Range<double> range, Range<double> otherRange)
        => range.IntersectWith(otherRange).Size() > 0;

    public static Range<double> MinMaxWith(this Range<double> range, Range<double> other)
        => (Math.Min(range.Start, other.Start), Math.Max(range.End, other.End));
    public static Range<double> MinMaxWith(this Range<double> range, double point)
        => (Math.Min(range.Start, point), Math.Max(range.End, point));

    public static Range<double> IntersectWith(this Range<double> range, Range<double> other)
    {
        var result = new Range<double>(Math.Max(range.Start, other.Start), Math.Min(range.End, other.End));
        return result.Size() < 0 ? default : result;
    }
}
