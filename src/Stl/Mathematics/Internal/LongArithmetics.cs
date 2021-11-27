using Stl.Extensibility;

namespace Stl.Mathematics.Internal;

[MatchFor(typeof(long), typeof(IArithmetics))]
public sealed class LongArithmetics : Arithmetics<long>
{
    public LongArithmetics() => One = 1;

    public override long Negative(long x) => -x;
    public override long Add(long a, long b) => a + b;
    public override long Mul(long a, double m) => (long) (a * m);
    public override long Mul(long a, long m) => a * m;

    public override long DivRem(long a, long b, out long reminder)
        => Math.DivRem(a, b, out reminder);

    public override long DivNonNegativeRem(long a, long b, out long reminder)
    {
        var d = Math.DivRem(a, b, out reminder);
        if (reminder < 0) {
            d--;
            reminder += b;
        }
        return d;
    }
}
