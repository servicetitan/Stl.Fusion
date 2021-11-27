using Stl.Mathematics;

namespace Stl.Tests.Mathematics;

public class ArithmeticsTest
{
    [Fact] public void IntTest() => Test(Arithmetics<int>.Default);
    [Fact] public void LongTest() => Test(Arithmetics<long>.Default);
    [Fact] public void DoubleTest() => Test(Arithmetics<double>.Default);
    [Fact] public void TimeSpanTest() => Test(Arithmetics<TimeSpan>.Default);
    [Fact] public void MomentTest() => Test(Arithmetics<Moment>.Default);

    [Fact]
    public void NoArithmeticsTest()
    {
        try {
            var _ = Arithmetics<Unit>.Default;
            false.Should().BeTrue("Exception wasn't thrown.");
        }
        catch (InvalidOperationException) {
            // Intended
        }
    }

    public void Test<T>(Arithmetics<T> a)
        where T : notnull
    {
        var one = a.One;
        var two = a.Mul(one, 2L);
        a.Mul(one, 2d).Should().Be(two);

        var eleven = a.Mul(one, 11L);
        var four = a.Mul(one, 4L);
        var d = a.DivRem(eleven, four, out var r);
        d.Should().Be(2);
        r.Should().Be(a.Mul(one, 3L));
        a.Mod(eleven, four).Should().Be(a.Mul(one, 3L));

        var nEleven = a.Negative(eleven);
        d = a.DivNonNegativeRem(nEleven, four, out r);
        d.Should().Be(-3);
        r.Should().Be(one);
        a.Mod(nEleven, four).Should().Be(one);
    }
}
