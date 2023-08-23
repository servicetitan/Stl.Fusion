using Stl.Testing.Collections;

namespace Stl.Tests.Time;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class ServerClockTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public void BasicTest()
    {
        var cpuClock = MomentClockSet.Default.CpuClock;
        var clock = new ServerClock();

        clock.WhenReady.IsCompleted.Should().BeFalse();
        (clock.Now - cpuClock.Now).Should().BeCloseTo(default, TimeSpan.FromMilliseconds(100));

        var offset = TimeSpan.FromSeconds(1);
        clock.Offset = offset;
        clock.WhenReady.IsCompleted.Should().BeTrue();
        (clock.Now - offset - cpuClock.Now).Should().BeCloseTo(default, TimeSpan.FromMilliseconds(100));

        offset = TimeSpan.FromSeconds(2);
        clock.Offset = offset;
        clock.WhenReady.IsCompleted.Should().BeTrue();
        (clock.Now - offset - cpuClock.Now).Should().BeCloseTo(default, TimeSpan.FromMilliseconds(100));
    }
}
