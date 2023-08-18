using Stl.Fusion.Tests.Services;
using Stl.OS;

namespace Stl.Fusion.Tests;

public class ScreenshotServiceClientTest : FusionTestBase
{
    public ScreenshotServiceClientTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        if (OSInfo.IsAnyUnix)
            // Screenshots don't work on Unix
            return;

        var epsilon = TimeSpan.FromSeconds(1);

        await using var serving = await WebHost.Serve();
        await Delay(0.25);
        var service = (ScreenshotService)Services.GetRequiredService<IScreenshotService>();
        var clientService = ClientServices.GetRequiredService<IScreenshotService>();

        var initialScreenshotCount = service.ScreenshotCount;
        for (var i = 0; i < 50; i++) {
            var startedAt = SystemClock.Now;
            var c = await Computed.Capture(() => clientService.GetScreenshot(100));
            var screenshot = c.Value;
            screenshot.Should().NotBeNull();
            var endedAt = SystemClock.Now;

            var callDuration = endedAt - startedAt;
            var delay = endedAt - screenshot.CapturedAt;
            Log?.LogInformation("Call duration: {Duration}, delay: {Delay}", callDuration, delay);

            delay.Should().BeLessThan(epsilon);
            await Task.Delay(TimeSpan.FromSeconds(0.05));
        }
        var screenshotCount = service.ScreenshotCount - initialScreenshotCount;
        screenshotCount.Should().BeLessThan(15);
    }
}
