using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class TimerTest(ITestOutputHelper @out) : FusionTestBase(@out)
{
    [Fact]
    public async Task BasicTest()
    {
        await using var serving = await WebHost.Serve();
        var tp = WebServices.GetRequiredService<ITimeService>();
        var ctp = ClientServices.GetRequiredService<ITimeService>();

        var cTime = await Computed.Capture(() => ctp.GetTime()).AsTask().WaitAsync(TimeSpan.FromMinutes(1));
        var count = 0;
        using var state = WebServices.StateFactory().NewComputed<DateTime>(
            FixedDelayer.Instant,
            async (_, ct) => await ctp.GetTime(ct));
        state.Updated += (s, _) => {
            Out.WriteLine($"Client: {s.Value}");
            count++;
        };

        await TestExt.WhenMet(
            () => count.Should().BeGreaterThan(2),
            TimeSpan.FromSeconds(5));
    }
}
