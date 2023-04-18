namespace Stl.Mathematics.Internal;

public sealed class MomentArithmetics : Arithmetics<Moment>
{
    public MomentArithmetics() => One = new(TimeSpan.FromSeconds(1));

    public override Moment Negative(Moment x) => new(-x.EpochOffsetTicks);
    public override Moment Add(Moment a, Moment b) => new(a.EpochOffsetTicks + b.EpochOffsetTicks);
    public override Moment Mul(Moment a, double m) => new((long) (a.EpochOffsetTicks * m));
    public override Moment Mul(Moment a, long m) => new(a.EpochOffsetTicks * m);

    public override long DivRem(Moment a, Moment b, out Moment reminder)
    {
        var result = Math.DivRem(a.EpochOffsetTicks, b.EpochOffsetTicks, out var r);
        reminder = new Moment(r);
        return result;
    }

    public override long DivNonNegativeRem(Moment a, Moment b, out Moment reminder)
    {
        var result = Math.DivRem(a.EpochOffsetTicks, b.EpochOffsetTicks, out var r);
        if (r < 0) {
            result--;
            r += b.EpochOffsetTicks;
        }
        reminder = new Moment(r);
        return result;
    }
}
