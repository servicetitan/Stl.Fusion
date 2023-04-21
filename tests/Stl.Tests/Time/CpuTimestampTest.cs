namespace Stl.Tests.Time;

public class CpuTimestampTest : TestBase
{
    public CpuTimestampTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        CpuTimestamp.TicksPerSecond.Should().Be(10_000_000);
        var now = CpuTimestamp.Now;
        await Task.Delay(100);
        CpuTimestamp.Elapsed(now).Should().BeGreaterThan(TimeSpan.FromMilliseconds(50));
        await Task.Delay(100);
        CpuTimestamp.Elapsed(now).Should().BeGreaterThan(TimeSpan.FromMilliseconds(150));
        CpuTimestamp.Elapsed(now).Should().BeLessThan(TimeSpan.FromMilliseconds(2000));
    }
}
