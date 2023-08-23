namespace Stl.Tests.Mathematics;

public class MathExtTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void GcdLcmTest()
    {
        Assert.Equal(1, MathExt.Gcd(3, 5));
        Assert.Equal(4, MathExt.Gcd(8, 12));

        Assert.Equal(15, MathExt.Lcm(5, 3));
        Assert.Equal(12, MathExt.Lcm(4, 6));
    }

    [Fact]
    public void PowTest()
    {
        int Add(int x, int y) => x + y;

        Assert.Equal(0, MathExt.FastPower(3, 0, 0, Add));
        Assert.Equal(3, MathExt.FastPower(3, 1, 0, Add));
        Assert.Equal(6, MathExt.FastPower(3, 2, 0, Add));
        Assert.Equal(9, MathExt.FastPower(3, 3, 0, Add));
        Assert.Equal(12, MathExt.FastPower(3, 4, 0, Add));
    }

    [Fact]
    public void FactorialTest()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => MathExt.Factorial(-1));
        Assert.Equal(1, MathExt.Factorial(0));
        Assert.Equal(1, MathExt.Factorial(1));
        Assert.Equal(2, MathExt.Factorial(2));
        Assert.Equal(6, MathExt.Factorial(3));
        Assert.Equal(24, MathExt.Factorial(4));
        MathExt.Factorial(1000);
        Assert.Throws<ArgumentOutOfRangeException>(() => MathExt.Factorial(10_001));
    }
}
