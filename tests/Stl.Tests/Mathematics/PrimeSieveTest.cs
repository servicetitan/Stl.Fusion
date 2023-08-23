namespace Stl.Tests.Mathematics;

public class PrimeSieveTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void BasicTest()
    {
        var sieve = new PrimeSieve();
        var range = Enumerable.Range(1, 10);
        foreach (var x in range)
            Out.WriteLine($"{x} => {sieve.IsPrime(x)}");
        Assert.Equal(new [] {
                true,
                false,
                true,
                false,
                true,
                false,
                true,
                false,
                false,
                false
            },
            Enumerable.Range(1, 10).Select(x => sieve.IsPrime(x)));
        Assert.False(sieve.IsPrime(1299825));
        Assert.True(sieve.IsPrime(1299827));
    }
}
