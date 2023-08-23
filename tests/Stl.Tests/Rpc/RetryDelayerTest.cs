namespace Stl.Tests.Rpc;

public class RetryDelaySeqTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void BasicTest()
    {
        var s = new RetryDelaySeq();
        s.Min.Should().Be(RetryDelaySeq.DefaultMin);
        s.Max.Should().Be(RetryDelaySeq.DefaultMax);
        s.Spread.Should().Be(RetryDelaySeq.DefaultSpread);
        s.Multiplier.Should().Be(RetryDelaySeq.DefaultMultiplier);

        s[-1].TotalSeconds.Should().Be(0);
        s[0].TotalSeconds.Should().Be(0);
        s[1].TotalSeconds.Should().BeApproximately(s.Min.TotalSeconds, s.Min.TotalSeconds * s.Spread * 2);
        s[100].TotalSeconds.Should().BeApproximately(s.Max.TotalSeconds, s.Max.TotalSeconds * s.Spread * 2);
    }
}
