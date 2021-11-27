using Stl.Extensibility;

namespace Stl.Mathematics.Internal;

[MatchFor(typeof(TimeSpan), typeof(IArithmetics))]
public sealed class TimeSpanArithmetics : Arithmetics<TimeSpan>
{
    public TimeSpanArithmetics() => One = TimeSpan.FromSeconds(1);

    public override TimeSpan Negative(TimeSpan x) => -x;
    public override TimeSpan Add(TimeSpan a, TimeSpan b) => a + b;
    public override TimeSpan Mul(TimeSpan a, double m)
        => TimeSpan.FromTicks((long) (a.Ticks * m));
    public override TimeSpan Mul(TimeSpan a, long m)
        => TimeSpan.FromTicks(a.Ticks * m);

    public override long DivRem(TimeSpan a, TimeSpan b, out TimeSpan reminder)
    {
        var result = Math.DivRem(a.Ticks, b.Ticks, out var r);
        reminder = TimeSpan.FromTicks(r);
        return result;
    }

    public override long DivNonNegativeRem(TimeSpan a, TimeSpan b, out TimeSpan reminder)
    {
        var result = Math.DivRem(a.Ticks, b.Ticks, out var r);
        if (r < 0) {
            result--;
            r += b.Ticks;
        }
        reminder = TimeSpan.FromTicks(r);
        return result;
    }
}
