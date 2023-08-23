namespace Stl.Tests.Time;

public class CpuTimestampTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public async Task BasicTest()
    {
        Out.WriteLine("Tick frequency: {0}", CpuTimestamp.TickFrequency);
        CpuTimestamp.TickFrequency.Should().BeGreaterThan(1);
        CpuTimestamp.TickDuration.Should().BeGreaterThan(0).And.BeLessThan(1);
        var startedAt = CpuTimestamp.Now;

        await Task.Delay(100);
        startedAt.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(50));

        await Task.Delay(100);
        startedAt.Elapsed.Should().BeGreaterThan(TimeSpan.FromMilliseconds(150));

        if (TestRunnerInfo.IsBuildAgent())
            return; // By some reason the next Elapsed value is ~ 16s in GitHub action

        startedAt.Elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(2000));
    }
}
