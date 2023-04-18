namespace Stl.Mathematics.Internal;

public sealed class IntArithmetics : Arithmetics<int>
{
    public IntArithmetics() => One = 1;

    public override int Negative(int x) => -x;
    public override int Add(int a, int b) => a + b;
    public override int Mul(int a, double m) => (int) (a * m);
    public override int Mul(int a, long m) => (int) (a * m);

    public override long DivRem(int a, int b, out int reminder)
        => Math.DivRem(a, b, out reminder);

    public override long DivNonNegativeRem(int a, int b, out int reminder)
    {
        var d = Math.DivRem(a, b, out reminder);
        if (reminder < 0) {
            d--;
            reminder += b;
        }
        return d;
    }
}
