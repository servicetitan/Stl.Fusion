namespace Stl.Mathematics.Internal;

public sealed class DoubleArithmetics : Arithmetics<double>
{
    public DoubleArithmetics() => One = 1d;

    public override double Negative(double x) => -x;
    public override double Add(double a, double b) => a + b;
    public override double Mul(double a, double m) => (long) (a * m);
    public override double Mul(double a, long m) => (long) (a * m);

    public override long DivRem(double a, double b, out double reminder)
    {
        var d = a / b;
        var result = Math.Floor(d);
        reminder = a - b * result;
        if (result < 0 && reminder != 0) {
            result += 1;
            reminder -= b;
        }
        return (long) result;
    }

    public override long DivNonNegativeRem(double a, double b, out double reminder)
    {
        var d = a / b;
        var result = Math.Floor(d);
        reminder = a - b * result;
        return (long) result;
    }
}
